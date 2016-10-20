using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using PatchKit.Api;
using PatchKit.Async;
using PatchKit.Unity.Api;
using PatchKit.Unity.Utilities;
using PatchKit.Unity.Web;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Patcher
{
    /// <summary>
    /// Patcher.
    /// </summary>
    public class Patcher : IDisposable
    {
        private readonly PatcherConfiguration _configuration;

        private readonly PatcherData _patcherData;

        private readonly HttpDownloader _httpDownloader;

        private readonly TorrentDownloader _torrentDownloader;

        private readonly Unarchiver _unarchiver;

        private readonly Librsync _librsync;

        private readonly ApiConnection _apiConnection;

        private AsyncCancellationTokenSource _cancellationTokenSource;

        private PatcherStatus _status;

        private Thread _thread;

        public event Action<Patcher> OnPatcherStarted;

        public event Action<Patcher> OnPatcherProgress;

        public event Action<Patcher> OnPatcherFinished;

        /// <summary>
        /// Initializes instance of <see cref="PatcherConfiguration"/>.
        /// Must be called from main Unity thread since it requires some initial configuration.
        /// </summary>
        /// <param name="configuration"></param>
        public Patcher(PatcherConfiguration configuration)
        {
            Dispatcher.Initialize();
            _configuration = configuration;
            _patcherData = new PatcherData(_configuration.ApplicationDataPath);
            _httpDownloader = new HttpDownloader();
            _torrentDownloader = new TorrentDownloader(10000);
            _unarchiver = new Unarchiver();
            _librsync = new Librsync();
            _apiConnection = ApiConnectionInstance.Instance;
            ResetStatus(PatcherState.None);
        }

        public PatcherStatus Status
        {
            get { return _status; }
        }

        public void Start()
        {
            if (_status.State == PatcherState.Patching)
            {
                throw new InvalidOperationException("Patching is already started.");
            }

            _cancellationTokenSource = new AsyncCancellationTokenSource();

            ResetStatus(PatcherState.Patching);

            _thread = new Thread(state =>
            {
                LogInfo("Invoking OnPatchingStarted event.");
                Dispatcher.Invoke(InvokeOnPatcherStarted);

                try
                {
                    LogInfo("Starting patching.");
                    Patch(_cancellationTokenSource.Token);

                    LogInfo("Setting status to Succeed.");
                    ResetStatus(PatcherState.Succeed);
                }
                catch (OperationCanceledException)
                {
                    LogInfo("Setting status to Cancelled.");
                    ResetStatus(PatcherState.Cancelled);
                }
                catch (Exception exception)
                {
                    LogInfo("Setting status to Failed.");
                    ResetStatus(PatcherState.Failed);
                    Debug.LogException(exception);
                }
                finally
                {
                    LogInfo("Invoking OnPatchingFinished event.");
                    Dispatcher.Invoke(InvokeOnPatcherFinished);
                }
            })
            {
                IsBackground = true
            };
            _thread.Start();
        }

        public void Cancel()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        bool ShouldDownloadContent(int currentVersion, int commonVersion)
        {
            if (currentVersion < commonVersion ||
                !CheckVersionConsistency(commonVersion))
            {
                LogInfo("Local version is corrupted. Redownloading content.");
                return true;
            }

            if (commonVersion < currentVersion)
            {
                // Calculate sum of diff size and compare it to the content size

                long contentSize = _apiConnection.GetAppVersionContentSummary(_configuration.AppSecret, currentVersion).Size;

                long sumDiffSize = 0;

                for (int v = commonVersion + 1; v <= currentVersion; v++)
                {
                    sumDiffSize += _apiConnection.GetAppVersionDiffSummary(_configuration.AppSecret, v).Size;

                    if (sumDiffSize >= contentSize)
                    {
                        LogInfo("Diff size is bigger than content size. Redownloading content.");
                        return true;
                    }
                }
            }

            return false;
        }

        bool ShouldDownloadDiff(int currentVersion, int commonVersion)
        {
            return currentVersion > commonVersion;
        }

        private void Patch(AsyncCancellationToken cancellationToken)
        {
            var progressTracker = new ProgressTracker();

            progressTracker.OnProgress += progress =>
            {
                _status.Progress = progress;
                Dispatcher.Invoke(InvokeOnPatcherProgress);
            };

            _status.Progress = 0.0f;

            LogInfo("Fetching current application version.");
            int currentVersion = _apiConnection.GetAppLatestVersionId(_configuration.AppSecret).Id;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_configuration.ForceVersion != 0)
            {
                LogInfo("Forcing application version: " + _configuration.ForceVersion);
                currentVersion = _configuration.ForceVersion;
            }
#endif

            LogInfo(string.Format("Fetched current version - {0}.", currentVersion));

            int? commonVersion = _patcherData.Cache.GetCommonVersion();

            LogInfo(string.Format("Common version of local application - {0}.", commonVersion.HasValue ? commonVersion.Value.ToString() : "none"));

            LogInfo("Comparing common local version with current version.");

            if (commonVersion == null || ShouldDownloadContent(currentVersion, commonVersion.Value))
            {
                CheckIfCurrentDirectoryIsWritable();

                LogInfo("Local application doesn't exist or files are corrupted.");

                LogInfo("Clearing local application data.");
                _patcherData.Clear();

                LogInfo("Downloading content of current version.");
                DownloadVersionContent(currentVersion, progressTracker, cancellationToken);
            }
            else if (ShouldDownloadDiff(currentVersion, commonVersion.Value))
            {
                CheckIfCurrentDirectoryIsWritable();

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

        private void DownloadPackage(string packagePath, string torrentUrl, string[] httpUrls, long packageSize, ProgressTracker.Task downloadProgress, AsyncCancellationToken cancellationToken)
        {
            string torrentPath = packagePath + ".torrent";

            try
            {
                LogInfo("Trying to download with torrent.");

                LogInfo(string.Format("Starting download of torrent file from {0} to {1}.", torrentUrl, torrentPath));
                _httpDownloader.DownloadFile(torrentUrl, torrentPath, 0, (progress, speed, bytes, totalBytes) => { },
                    cancellationToken);

                LogInfo("Torrent file has been downloaded.");

                LogInfo(string.Format("Starting torrent download of package to {0}.", packagePath));

                _torrentDownloader.DownloadFile(torrentPath, packagePath,
                    (progress, speed, bytes, totalBytes) =>
                        OnDownloadProgress(downloadProgress, progress, speed, bytes, totalBytes), cancellationToken);
            }
            catch (Exception exception)
            {
                LogError(exception.ToString());
                LogWarning("Failed to download with torrent.");
                LogInfo("Trying to download with HTTP.");

                bool downloaded = false;

                foreach (var url in httpUrls)
                {
                    try
                    {
                        LogInfo(string.Format("Starting HTTP download of content package file from {0} to {1}.", url,
                            packagePath));
                        _httpDownloader.DownloadFile(url, packagePath, packageSize,
                            (progress, speed, bytes, totalBytes) =>
                                OnDownloadProgress(downloadProgress, progress, speed, bytes, totalBytes),
                            cancellationToken);
                        downloaded = true;
                        break;
                    }
                    catch (Exception exception2)
                    {
                        LogError(exception2.ToString());
                        LogInfo(
                            string.Format(
                                "Failed to HTTP download content package file from {0} to {1}. Trying next url.", url,
                                packagePath));
                    }
                }

                if (!downloaded)
                {
                    throw new Exception("Failed to download content package from any sources.");
                }
            }
            finally
            {
                LogInfo("Cleaning up package downloading.");

                if (File.Exists(torrentPath))
                {
                    File.Delete(torrentPath);
                }
            }
        }

        private void DownloadVersionContent(int version, ProgressTracker progressTracker, AsyncCancellationToken cancellationToken)
        {
            var downloadProgress = progressTracker.AddNewTask(2.0f);
            var unzipProgress = progressTracker.AddNewTask(0.5f);

            LogInfo("Fetching content summary.");
            var contentSummary = _apiConnection.GetAppVersionContentSummary(_configuration.AppSecret, version);

            LogInfo("Fetching content torrent url.");
            var contentTorrentUrl = _apiConnection.GetAppVersionContentTorrentUrl(_configuration.AppSecret, version);

            LogInfo("Fetching content urls.");
            var contentUrls = _apiConnection.GetAppVersionContentUrls(_configuration.AppSecret, version).Select(url => url.Url).ToArray();

            var contentPackagePath = Path.Combine(_patcherData.TempPath, string.Format("download-content-{0}.package", version));

            try
            {
                _status.IsDownloading = true;

                LogInfo("Downloading content package.");

                DownloadPackage(contentPackagePath, contentTorrentUrl.Url, contentUrls, contentSummary.Size,
                    downloadProgress, cancellationToken);

                LogInfo("Content package has been downloaded.");

                _status.IsDownloading = false;

                LogInfo(string.Format("Unarchiving content package to {0}.", _patcherData.Path));

                _unarchiver.Unarchive(contentPackagePath, _patcherData.Path, progress => unzipProgress.Progress = progress, cancellationToken);

                LogInfo("Content has been unarchived.");

                LogInfo("Saving content files version to cache.");

                foreach (var contentFile in contentSummary.Files)
                {
                    _patcherData.Cache.SetFileVersion(contentFile.Path, version);
                }
            }
            finally
            {
                LogInfo("Cleaning up after content downloading.");

                if (File.Exists(contentPackagePath))
                {
                    File.Delete(contentPackagePath);
                }
            }
        }

        private void DownloadVersionDiff(int version, ProgressTracker progressTracker, AsyncCancellationToken cancellationToken)
        {
            var downloadProgress = progressTracker.AddNewTask(2.0f);
            var unzipProgress = progressTracker.AddNewTask(0.5f);
            var patchProgress = progressTracker.AddNewTask(1.0f);

            LogInfo("Fetching diff summary.");
            var diffSummary = _apiConnection.GetAppVersionDiffSummary(_configuration.AppSecret, version);

            LogInfo("Fetching diff torrent url.");
            var diffTorrentUrl = _apiConnection.GetAppVersionDiffTorrentUrl(_configuration.AppSecret, version);

            LogInfo("Fetching diff urls.");
            var diffUrls = _apiConnection.GetAppVersionDiffUrls(_configuration.AppSecret, version).Select(url => url.Url).ToArray();

            var diffPackagePath = Path.Combine(_patcherData.TempPath, string.Format("download-diff-{0}.package", version));

            var diffDirectoryPath = Path.Combine(_patcherData.TempPath, string.Format("diff-{0}", version));

            try
            {
                _status.IsDownloading = true;

                LogInfo("Downloading diff package.");

                DownloadPackage(diffPackagePath, diffTorrentUrl.Url, diffUrls, diffSummary.Size, downloadProgress,
                    cancellationToken);

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
                    // Skip directories
                    if (removedFile.EndsWith("/"))
                    {
                        continue;
                    }

                    LogInfo(string.Format("Deleting file {0}", removedFile));

                    _patcherData.ClearFile(removedFile);

                    doneFilesCount++;

                    patchProgress.Progress = (float)doneFilesCount/totalFilesCount;
                }

                foreach (var removedFile in diffSummary.RemovedFiles)
                {
                    if (removedFile.EndsWith("/"))
                    {
                        LogInfo(string.Format("Trying to clear directory {0}", removedFile));

                        if (_patcherData.TryClearDirectory(removedFile))
                        {
                            LogInfo(string.Format("Succesfuly cleared directory {0}", removedFile));
                        }
                        else
                        {
                            LogInfo(string.Format("Unable to clear directory {0}", removedFile));
                        }
                    }
                }

                foreach (var addedFile in diffSummary.AddedFiles)
                {
                    LogInfo(string.Format("Adding file {0}", addedFile));

                    
                    if (addedFile.EndsWith("/"))
                    {
                        string directoryPath = Path.Combine(diffDirectoryPath,
                            addedFile.Substring(addedFile.Length - 1));

                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        continue;
                    }
                    
                    string sourceFilePath = Path.Combine(diffDirectoryPath, addedFile);

                    string destinationFilePath = _patcherData.GetFilePath(addedFile);

                    string destinationDirPath = Path.GetDirectoryName(destinationFilePath);

                    // ReSharper disable once AssignNullToNotNullAttribute
                    if (!Directory.Exists(destinationDirPath))
                    {
                        Directory.CreateDirectory(destinationDirPath);
                    }

                    File.Copy(sourceFilePath, destinationFilePath, true);

                    _patcherData.Cache.SetFileVersion(addedFile, version);

                    doneFilesCount++;

                    patchProgress.Progress = (float)doneFilesCount / totalFilesCount;
                }

                foreach (var modifiedFile in diffSummary.ModifiedFiles)
                {
                    LogInfo(string.Format("Patching file {0}", modifiedFile));

                    // HACK: Workaround for directories included in diff summary.
                    if (Directory.Exists(_patcherData.GetFilePath(modifiedFile)))
                    {
                        continue;
                    }

                    _patcherData.Cache.SetFileVersion(modifiedFile, -1);

                    _librsync.Patch(_patcherData.GetFilePath(modifiedFile), Path.Combine(diffDirectoryPath, modifiedFile), cancellationToken);

                    _patcherData.Cache.SetFileVersion(modifiedFile, version);

                    doneFilesCount++;

                    patchProgress.Progress = (float)doneFilesCount / totalFilesCount;
                }

                patchProgress.Progress = 1.0f;
            }
            finally
            {
                LogInfo("Cleaning up after diff downloading.");

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
            var commonVersionContentSummary = _apiConnection.GetAppVersionContentSummary(_configuration.AppSecret, version);

            return _patcherData.CheckFilesConsistency(version, commonVersionContentSummary);
        }

        private void OnDownloadProgress(ProgressTracker.Task downloadTaskProgress, float progress, float speed, long bytes, long totalBytes)
        {
            _status.DownloadSpeed = speed;
            _status.DownloadProgress = progress;
            _status.DownloadBytes = bytes;
            _status.DownloadTotalBytes = totalBytes;
            downloadTaskProgress.Progress = progress;
        }

        private void ResetStatus(PatcherState state)
        {
            _status.Progress = 1.0f;

            _status.IsDownloading = false;
            _status.DownloadProgress = 1.0f;
            _status.DownloadSpeed = 0.0f;

            _status.State = state;
        }

        private const string LogMessageFormat = "[Patcher] {0}";

        private static void LogInfo(string message)
        {
            Debug.Log(string.Format(LogMessageFormat, message));
        }

        // ReSharper disable once UnusedMember.Local
        private static void LogError(string message)
        {
            Debug.LogError(string.Format(LogMessageFormat, message));
        }

        // ReSharper disable once UnusedMember.Local
        private static void LogWarning(string message)
        {
            Debug.LogWarning(string.Format(LogMessageFormat, message));
        }

        public void Dispose()
        {
            if (_thread != null && _thread.IsAlive)
            {
                Debug.LogWarning("Patcher thread wasn't cancelled so it had to be aborted.");
                _thread.Abort();
            }
        }

        protected virtual void InvokeOnPatcherStarted()
        {
            if (OnPatcherStarted != null)
            {
                OnPatcherStarted(this);
            }
        }

        protected virtual void InvokeOnPatcherProgress()
        {
            if (OnPatcherProgress != null)
            {
                OnPatcherProgress(this);
            }
        }

        protected virtual void InvokeOnPatcherFinished()
        {
            if (OnPatcherFinished != null)
            {
                OnPatcherFinished(this);
            }
        }

        private void CheckIfCurrentDirectoryIsWritable()
        {
            bool isWritable;

            try
            {
                string permissionsCheckFilePath = Path.Combine(_configuration.ApplicationDataPath, ".permissions_check");

                if (!Directory.Exists(_configuration.ApplicationDataPath))
                {
                    Directory.CreateDirectory(_configuration.ApplicationDataPath);
                }

                using (FileStream fs = new FileStream(permissionsCheckFilePath, FileMode.CreateNew,
                                                            FileAccess.Write))
                {
                    fs.WriteByte(0xff);
                }

                if (File.Exists(permissionsCheckFilePath))
                {
                    File.Delete(permissionsCheckFilePath);
                    isWritable = true;
                }
                else
                {
                    isWritable = false;
                }
            }
            catch (Exception)
            {
                isWritable = false;
            }

            if (!isWritable)
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    ProcessStartInfo info = new ProcessStartInfo
                    {
                        FileName = Application.dataPath.Replace("_Data", ".exe"),
                        Arguments = string.Join(" ", Environment.GetCommandLineArgs().Select(s => "\"" + s + "\"").ToArray()),
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    Process.Start(info);

                    Application.Quit();
                    throw new OperationCanceledException();
                }

                throw new UnauthorizedAccessException("Missing write access for working directory - " +
                                                      _configuration.ApplicationDataPath);
            }
        }
    }
}
