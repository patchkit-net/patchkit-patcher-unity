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
    public class PatchKitUnityPatcher : MonoBehaviour
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
                Dispatcher.Invoke(OnPatchingStarted.Invoke);
                try
                {
                    Patch(_cancellationTokenSource.Token);
                    _status.State = PatcherState.Succeed;
                }
                catch (NoInternetConnectionException exception)
                {
                    _status.State = PatcherState.NoInternetConnection;
                    Debug.LogException(exception);
                }
                catch (OperationCanceledException)
                {
                    _status.State = PatcherState.Cancelled;
                }
                catch (Exception exception)
                {
                    _status.State = PatcherState.Failed;
                    Debug.LogException(exception);
                }
                finally
                {
                    ResetStatus();
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

            if (!InternetConnectionTester.CheckInternetConnection(cancellationToken))
            {
                throw new NoInternetConnectionException();
            }

            int currentVersion = _api.GetAppLatestVersionId(_secretKey).Id;

            int? commonVersion = _applicationData.Cache.GetCommonVersion();

            if (commonVersion == null || currentVersion < commonVersion.Value || !CheckVersionConsistency(commonVersion.Value))
            {
                _applicationData.Clear();

                DownloadVersionContent(currentVersion, progressTracker, cancellationToken);
            }
            else if (commonVersion.Value != currentVersion)
            {
                while (currentVersion > commonVersion.Value)
                {
                    commonVersion = commonVersion.Value + 1;

                    DownloadVersionDiff(commonVersion.Value, progressTracker, cancellationToken);
                }
            }
        }

        private void DownloadVersionContent(int version, ProgressTracker progressTracker, AsyncCancellationToken cancellationToken)
        {
            var downloadTorrentProgress = progressTracker.AddNewTask(0.05f);
            var downloadProgress = progressTracker.AddNewTask(2.0f);
            var unzipProgress = progressTracker.AddNewTask(0.5f);

            var contentSummary = _api.GetAppContentSummary(_secretKey, version);

            var contentTorrentUrl = _api.GetAppContentTorrentUrl(_secretKey, version);

            var contentPackagePath = Path.Combine(_applicationData.TempPath, string.Format("download-content-{0}.package", version));

            var contentTorrentPath = Path.Combine(_applicationData.TempPath, string.Format("download-content-{0}.torrent", version));

            try
            {
                _status.IsDownloading = true;

                _httpDownloader.DownloadFile(contentTorrentUrl.Url, contentTorrentPath, 0, (progress, speed) => OnDownloadProgress(downloadTorrentProgress, progress, speed),
                    cancellationToken);

                _torrentDownloader.DownloadFile(contentTorrentPath, contentPackagePath, (progress, speed) => OnDownloadProgress(downloadProgress, progress, speed),
                    cancellationToken);

                _status.IsDownloading = false;

                _unarchiver.Unarchive(contentPackagePath, _applicationData.Path, progress => unzipProgress.Progress = progress, cancellationToken);

                foreach (var contentFile in contentSummary.Files)
                {
                    _applicationData.Cache.SetFileVersion(contentFile.Path, version);
                }
            }
            finally
            {
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

            var diffSummary = _api.GetAppDiffSummary(_secretKey, version);

            var diffTorrentUrl = _api.GetAppDiffTorrentUrl(_secretKey, version);

            var diffPackagePath = Path.Combine(_applicationData.TempPath, string.Format("download-diff-{0}.package", version));

            var diffTorrentPath = Path.Combine(_applicationData.TempPath, string.Format("download-diff-{0}.torrent", version));

            var diffDirectoryPath = Path.Combine(_applicationData.TempPath, string.Format("diff-{0}", version));

            try
            {
                _status.IsDownloading = true;

                _httpDownloader.DownloadFile(diffTorrentUrl.Url, diffTorrentPath, 0, (progress, speed) => OnDownloadProgress(downloadTorrentProgress, progress, speed), cancellationToken);

                _torrentDownloader.DownloadFile(diffTorrentPath, diffPackagePath, (progress, speed) => OnDownloadProgress(downloadProgress, progress, speed), cancellationToken);

                _unarchiver.Unarchive(diffPackagePath, diffDirectoryPath, progress => unzipProgress.Progress = progress, cancellationToken);

                _status.IsDownloading = false;

                int totalFilesCount = diffSummary.RemovedFiles.Length + diffSummary.AddedFiles.Length +
                                        diffSummary.ModifiedFiles.Length;

                int doneFilesCount = 0;

                patchProgress.Progress = 0.0f;

                foreach (var removedFile in diffSummary.RemovedFiles)
                {
                    _applicationData.ClearFile(removedFile);

                    doneFilesCount++;

                    patchProgress.Progress = (float)doneFilesCount/totalFilesCount;
                }

                foreach (var addedFile in diffSummary.AddedFiles)
                {
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
    }
}