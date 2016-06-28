using System;
using System.IO;
using System.Threading;
using PatchKit.API;
using PatchKit.API.Async;
using PatchKit.Unity.API;
using PatchKit.Unity.Common;
using PatchKit.Unity.Patcher.Application;
using PatchKit.Unity.Patcher.Utilities;
using PatchKit.Unity.Patcher.Web;
using UnityEngine;
using UnityEngine.Events;

namespace PatchKit.Unity.Patcher
{
    /// <summary>
    /// Patcher working in Unity.
    /// </summary>
    public class Patcher : MonoBehaviour
    {
        // Configuration

        public string SecretKey;

        public string ApplicationDataPath;

        // Events

        public UnityEvent OnPatchingStarted;

        public UnityEvent OnPatchingProgress;

        public UnityEvent OnPatchingFinished;

        // Status

        public PatcherStatus Status
        {
            get { return _status; }
        }

        private PatcherStatus _status;

        // Variables used during patching

        private string _secretKey;

        private AsyncCancellationTokenSource _cancellationTokenSource;

        private PatchKitAPI _api;

        private ApplicationData _applicationData;

        private HttpDownloader _httpDownloader;

        private TorrentDownloader _torrentDownloader;

        private Unarchiver _unarchiver;

        private Librsync _librsync;

        private void Awake()
        {
            Dispatcher.Initialize();

            ResetStatus();
            _status.State = PatcherState.None;
        }

        private void OnDestroy()
        {
            CancelPatching();
        }

        private void OnApplicationQuit()
        {
            if (_status.State == PatcherState.Patching)
            {
                CancelPatching();

                UnityEngine.Application.CancelQuit();
            }
        }

        public void StartPatching()
        {
            if (_status.State == PatcherState.Patching)
            {
                throw new InvalidOperationException("Patching is already started.");
            }

            _secretKey = SecretKey;

            _cancellationTokenSource = new AsyncCancellationTokenSource();

            _api = PatchKitUnity.API;

            _applicationData = new ApplicationData(ApplicationDataPath);

            _httpDownloader = new HttpDownloader();

            _torrentDownloader = new TorrentDownloader();

            _unarchiver = new Unarchiver();

            _librsync = new Librsync();

            _status.State = PatcherState.Patching;

            ThreadPool.QueueUserWorkItem(state =>
            {
                LogInfo("Invoking OnPatchingStarted event.");
                Dispatcher.Invoke(OnPatchingStarted.Invoke);

                try
                {
                    LogInfo("Starting patching.");
                    Patch(_cancellationTokenSource.Token);

                    LogInfo("Setting status to Succeed.");
                    _status.State = PatcherState.Succeed;
                }
                catch (NoInternetConnectionException exception)
                {
                    LogInfo("Setting status to NoInternetConnection.");
                    _status.State = PatcherState.NoInternetConnection;

                    Debug.LogException(exception);
                }
                catch (OperationCanceledException)
                {
                    LogInfo("Setting status to Cancelled.");
                    _status.State = PatcherState.Cancelled;
                }
                catch (Exception exception)
                {
                    LogInfo("Setting status to Failed.");
                    _status.State = PatcherState.Failed;
                    Debug.LogException(exception);
                }
                finally
                {
                    ResetStatus();

                    LogInfo("Invoking OnPatchingFinished event.");
                    Dispatcher.Invoke(OnPatchingFinished.Invoke);
                }
            });
        }

        public void CancelPatching()
        {
            _cancellationTokenSource.Cancel();
        }

        private void Patch(AsyncCancellationToken cancellationToken)
        {
            var progressTracker = new ProgressTracker();

            progressTracker.OnProgress += progress =>
            {
                _status.Progress = progress;
                Dispatcher.Invoke(OnPatchingProgress.Invoke);
            };

            _status.Progress = 0.0f;

            LogInfo("Checking internet connection.");
            if (!InternetConnectionTester.CheckInternetConnection(cancellationToken))
            {
                LogError("No internet connection.");
                throw new NoInternetConnectionException();
            }


            LogInfo("Fetching current application version.");
            int currentVersion = _api.GetAppLatestVersionId(_secretKey).Id;

            LogInfo(string.Format("Fetched current version - {0}.", currentVersion));

            int? commonVersion = _applicationData.Cache.GetCommonVersion();

            LogInfo(string.Format("Common version of local application - {0}.", commonVersion.HasValue ? commonVersion.Value.ToString() : "none"));

            LogInfo("Comparing common local version with current version.");

            if (commonVersion == null || currentVersion < commonVersion.Value || !CheckVersionConsistency(commonVersion.Value))
            {
                LogInfo("Local application doesn't exist or files are corrupted.");

                LogInfo("Clearing local application data.");
                _applicationData.Clear();

                LogInfo("Downloading content of current version.");
                DownloadVersionContent(currentVersion, progressTracker, cancellationToken);
            }
            else if (commonVersion.Value != currentVersion)
            {
                LogInfo("Patching local application.");
                while (currentVersion > commonVersion.Value)
                {
                    LogInfo(string.Format("Patching from version {0}.", commonVersion.Value));

                    commonVersion = commonVersion.Value + 1;

                    DownloadVersionDiff(commonVersion.Value, progressTracker, cancellationToken);
                }
            }

            LogInfo("Application is up to date.");
        }

        private void DownloadVersionContent(int version, ProgressTracker progressTracker, AsyncCancellationToken cancellationToken)
        {
            var downloadTorrentProgress = progressTracker.AddNewTask(0.05f);
            var downloadProgress = progressTracker.AddNewTask(2.0f);
            var unzipProgress = progressTracker.AddNewTask(0.5f);

            LogInfo("Fetching content summary.");
            var contentSummary = _api.GetAppContentSummary(_secretKey, version);

            LogInfo("Fetching content torrent url.");
            var contentTorrentUrl = _api.GetAppContentTorrentUrl(_secretKey, version);

            var contentPackagePath = Path.Combine(_applicationData.TempPath, string.Format("download-content-{0}.package", version));

            var contentTorrentPath = Path.Combine(_applicationData.TempPath, string.Format("download-content-{0}.torrent", version));

            try
            {
                _status.IsDownloading = true;

                LogInfo(string.Format("Starting download of content torrent file from {0} to {1}.", contentTorrentUrl.Url, contentTorrentPath));
                _httpDownloader.DownloadFile(contentTorrentUrl.Url, contentTorrentPath, 0, (progress, speed) => OnDownloadProgress(downloadTorrentProgress, progress, speed),
                    cancellationToken);

                LogInfo("Content torrent file has been downloaded.");

                LogInfo(string.Format("Starting download of content package to {0}.", contentPackagePath));

                _torrentDownloader.DownloadFile(contentTorrentPath, contentPackagePath, (progress, speed) => OnDownloadProgress(downloadProgress, progress, speed),
                    cancellationToken);

                LogInfo("Content package has been downloaded.");

                _status.IsDownloading = false;

                LogInfo(string.Format("Unarchiving content package to {0}.", _applicationData.Path));

                _unarchiver.Unarchive(contentPackagePath, _applicationData.Path, progress => unzipProgress.Progress = progress, cancellationToken);

                LogInfo("Content has been unarchived.");

                LogInfo("Saving content files version to cache.");

                foreach (var contentFile in contentSummary.Files)
                {
                    _applicationData.Cache.SetFileVersion(contentFile.Path, version);
                }
            }
            finally
            {
                LogInfo("Cleaning up after content downloading.");

                if (File.Exists(contentTorrentPath))
                {
                    File.Delete(contentTorrentPath);
                }

                if (File.Exists(contentPackagePath))
                {
                    File.Delete(contentPackagePath);
                }
            }
        }

        private void DownloadVersionDiff(int version, ProgressTracker progressTracker, AsyncCancellationToken cancellationToken)
        {
            var downloadTorrentProgress = progressTracker.AddNewTask(0.05f);
            var downloadProgress = progressTracker.AddNewTask(2.0f);
            var unzipProgress = progressTracker.AddNewTask(0.5f);
            var patchProgress = progressTracker.AddNewTask(1.0f);

            LogInfo("Fetching diff summary.");
            var diffSummary = _api.GetAppDiffSummary(_secretKey, version);

            LogInfo("Fetching diff torrent url.");
            var diffTorrentUrl = _api.GetAppDiffTorrentUrl(_secretKey, version);

            var diffPackagePath = Path.Combine(_applicationData.TempPath, string.Format("download-diff-{0}.package", version));

            var diffTorrentPath = Path.Combine(_applicationData.TempPath, string.Format("download-diff-{0}.torrent", version));

            var diffDirectoryPath = Path.Combine(_applicationData.TempPath, string.Format("diff-{0}", version));

            try
            {
                _status.IsDownloading = true;

                LogInfo(string.Format("Starting download of diff torrent file from {0} to {1}.", diffTorrentUrl.Url, diffTorrentPath));
                _httpDownloader.DownloadFile(diffTorrentUrl.Url, diffTorrentPath, 0, (progress, speed) => OnDownloadProgress(downloadTorrentProgress, progress, speed), cancellationToken);

                LogInfo("Diff torrent file has been downloaded.");

                LogInfo(string.Format("Starting download of diff package to {0}.", diffPackagePath));
                _torrentDownloader.DownloadFile(diffTorrentPath, diffPackagePath, (progress, speed) => OnDownloadProgress(downloadProgress, progress, speed), cancellationToken);

                LogInfo("Diff package has been downloaded.");

                _status.IsDownloading = false;

                LogInfo(string.Format("Unarchiving diff package to {0}.", diffDirectoryPath));

                _unarchiver.Unarchive(diffPackagePath, diffDirectoryPath, progress => unzipProgress.Progress = progress, cancellationToken);

                int totalFilesCount = diffSummary.RemovedFiles.Length + diffSummary.AddedFiles.Length +
                                        diffSummary.ModifiedFiles.Length;

                int doneFilesCount = 0;

                patchProgress.Progress = 0.0f;

                foreach (var removedFile in diffSummary.RemovedFiles)
                {
                    LogInfo(string.Format("Deleting file {0}", removedFile));

                    _applicationData.ClearFile(removedFile);

                    doneFilesCount++;

                    patchProgress.Progress = (float)doneFilesCount/totalFilesCount;
                }

                foreach (var addedFile in diffSummary.AddedFiles)
                {
                    LogInfo(string.Format("Adding file {0}", addedFile));

                    // HACK: Workaround for directories included in diff summary.
                    if (Directory.Exists(Path.Combine(diffDirectoryPath, addedFile)))
                    {
                        continue;
                    }

                    File.Copy(Path.Combine(diffDirectoryPath, addedFile), _applicationData.GetFilePath(addedFile), true);

                    _applicationData.Cache.SetFileVersion(addedFile, version);

                    doneFilesCount++;

                    patchProgress.Progress = (float)doneFilesCount / totalFilesCount;
                }

                foreach (var modifiedFile in diffSummary.ModifiedFiles)
                {
                    LogInfo(string.Format("Patching file {0}", modifiedFile));

                    // HACK: Workaround for directories included in diff summary.
                    if (Directory.Exists(_applicationData.GetFilePath(modifiedFile)))
                    {
                        continue;
                    }

                    _applicationData.Cache.SetFileVersion(modifiedFile, -1);

                    _librsync.Patch(_applicationData.GetFilePath(modifiedFile), Path.Combine(diffDirectoryPath, modifiedFile), cancellationToken);

                    _applicationData.Cache.SetFileVersion(modifiedFile, version);

                    doneFilesCount++;

                    patchProgress.Progress = (float)doneFilesCount / totalFilesCount;
                }

                patchProgress.Progress = 1.0f;
            }
            finally
            {
                LogInfo("Cleaning up after diff downloading.");

                if (File.Exists(diffTorrentPath))
                {
                    File.Delete(diffTorrentPath);
                }

                if (File.Exists(diffPackagePath))
                {
                    File.Delete(diffPackagePath);
                }

                if (Directory.Exists(diffDirectoryPath))
                {
                    Directory.Delete(diffDirectoryPath, true);
                }
            }
        }

        private bool CheckVersionConsistency(int version)
        {
            var commonVersionContentSummary = _api.GetAppContentSummary(_secretKey, version);

            return _applicationData.CheckFilesConsistency(version, commonVersionContentSummary);
        }

        private void OnDownloadProgress(ProgressTracker.Task downloadTaskProgress, float progress, float speed)
        {
            _status.DownloadSpeed = speed;
            _status.DownloadProgress = progress;
            downloadTaskProgress.Progress = progress;
        }

        private void ResetStatus()
        {
            _status.Progress = 1.0f;

            _status.IsDownloading = false;
            _status.DownloadProgress = 1.0f;
            _status.DownloadSpeed = 0.0f;
        }

        private const string LogMessageFormat = "[Patcher] {0}";

        private void LogInfo(string message)
        {
            Debug.Log(string.Format(LogMessageFormat, message));
        }

        private void LogError(string message)
        {
            Debug.LogError(string.Format(LogMessageFormat, message));
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning(string.Format(LogMessageFormat, message));
        }
    }
}