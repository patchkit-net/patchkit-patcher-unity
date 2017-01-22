using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using PatchKit.Api;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data;
using PatchKit.Unity.Patcher.Licensing;
using PatchKit.Unity.Patcher.Net;
using PatchKit.Unity.Patcher.Statistics;
using PatchKit.Unity.Utilities;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Patcher
{
    /// <summary>
    /// Patcher.
    /// </summary>
    public class Patcher : IDisposable
    {
        private readonly PatcherConfiguration _configuration;

        private LocalAppData _localAppData;

        private RemoteAppData _remoteAppData;

        private CancellationTokenSource _cancellationTokenSource;

        private Thread _thread;

        private PatcherState _state = PatcherState.None;

        public event Action<PatcherState> OnStateChanged;

        public event ProgressHandler OnProgress;

        public event CustomProgressHandler<DownloadProgress> OnDownloadProgress;

        private KeyLicenseObtainer _keyLicenseObtainer;

        private bool _canPlay;

        /// <summary>
        /// Initializes instance of <see cref="PatcherConfiguration"/>.
        /// Must be called from main Unity thread since it requires some initial configuration.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="keyLicenseObtainer"></param>
        public Patcher(PatcherConfiguration configuration, KeyLicenseObtainer keyLicenseObtainer)
        {
            Dispatcher.Initialize();

            _configuration = configuration;
            _keyLicenseObtainer = keyLicenseObtainer;
        }

        public PatcherState State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    _state = value;

                    Debug.Log(string.Format("Patcher state changed - {0}", _state));

                    InvokeOnStateChanged(_state);
                }
            }
        }

        public bool CanPlay
        {
            get { return State != PatcherState.Processing && _canPlay && (_configuration.AllowToPlayWithoutNewestVersion || State == PatcherState.Success); }
        }

        public void Start()
        {
            if (_thread != null && _thread.IsAlive)
            {
                throw new InvalidOperationException("Patching is already started.");
            }

            _localAppData = new LocalAppData(_configuration.ApplicationDataPath, Path.Combine(_configuration.ApplicationDataPath, ".temp"), _configuration.AppSecret);
            var keysApiConnection = new KeysApiConnection(Settings.GetKeysApiConnectionSettings());
            var keyLicenseValidator = new KeyLicenseValidator(_configuration.AppSecret, keysApiConnection);
            _remoteAppData = new RemoteAppData(_configuration.AppSecret, _keyLicenseObtainer, keyLicenseValidator);
            _cancellationTokenSource = new CancellationTokenSource();

            _thread = new Thread(state =>
            {
                PatcherState patcherState = State;

                try
                {
                    Process(_cancellationTokenSource.Token);

                    patcherState = PatcherState.Success;
                }
                catch (UnauthorizedAccessException)
                {
                    patcherState = PatcherState.UnauthorizedAccess;
                }
                catch (OperationCanceledException)
                {
                    patcherState = PatcherState.Cancelled;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);

                    patcherState = PatcherState.Error;
                }
                finally
                {
                    _canPlay = _localAppData.IsInstalled();
                    State = patcherState;
                }
            })
            {
                IsBackground = true
            };

            Debug.Log("Starting patcher thread.");

            _thread.Start();
        }

        public void Cancel()
        {
            if (_cancellationTokenSource != null)
            {
                Debug.Log("Cancelling patcher.");

                _cancellationTokenSource.Cancel();
            }
        }

        private bool CanUpdateWithDiff(int localVersionId, int latestVersionId)
        {
            Debug.Log(string.Format("Checking whether processing version {0} with diff to {1} is possible.", localVersionId, latestVersionId));

            var localVersionContentSummary = _remoteAppData.GetContentSummary(localVersionId);

            if (!_localAppData.CheckDataConsistency(localVersionContentSummary, localVersionId))
            {
                Debug.Log("Local application version is corrupted.");

                return false;
            }

            long contentSize = _remoteAppData.GetContentSummary(latestVersionId).Value<long>("size");

            long sumDiffSize = 0;

            for (int v = localVersionId + 1; v <= latestVersionId; v++)
            {
                sumDiffSize += _remoteAppData.GetDiffSummary(v).Value<long>("size");

                if (sumDiffSize >= contentSize)
                {
                    Debug.Log("Downloading content is more worth than downloading diffs.");

                    return false;
                }
            }

            return true;
        }

        private void UpdateWithDiff(int localVersionId, int latestVersionId, ComplexProgressReporter progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log(string.Format("Updating application of version {0} with diff to version {1}.", localVersionId, latestVersionId));

            var downloadDiffPackageProgressReporters = new Dictionary<int, CustomProgressReporter<DownloadProgress>>();
            var patchProgressReporters = new Dictionary<int, ComplexProgressReporter>();
            var diffSummaries = new Dictionary<int, JObject>();
            var contentSummaries = new Dictionary<int, JObject>();

            contentSummaries[localVersionId] = _remoteAppData.GetContentSummary(localVersionId);

            for (int v = localVersionId + 1; v <= latestVersionId; v++)
            {
                var diffSummary = _remoteAppData.GetDiffSummary(v);
                diffSummaries.Add(v, diffSummary);

                var contentSummary = _remoteAppData.GetContentSummary(v);
                contentSummaries.Add(v, contentSummary);

                var dr = new CustomProgressReporter<DownloadProgress>();
                downloadDiffPackageProgressReporters.Add(v, dr);
                progressReporter.AddChild(dr, diffSummary.Value<long>("size"));
                dr.OnProgress += InvokeOnDownloadProgress;

                var pr = new ComplexProgressReporter();
                patchProgressReporters.Add(v, pr);
                progressReporter.AddChild(pr, diffSummary.Value<long>("size") * 0.25);
            }

            for (int v = localVersionId + 1; v <= latestVersionId; v++)
            {
                using (var temporaryDirectory = new TemporaryStorage(Path.Combine(_localAppData.TemporaryPath, string.Format("diff-{0}-download", v))))
                {
                    string diffPackagePath = Path.Combine(temporaryDirectory.Path,
                        string.Format("diff-{0}.package", v));

                    _remoteAppData.DownloadDiffPackage(diffPackagePath, v,
                        downloadDiffPackageProgressReporters[v], cancellationToken);

                    _localAppData.Patch(diffPackagePath, diffSummaries[v], contentSummaries[v-1], v,
                        patchProgressReporters[v], cancellationToken);
                }
            }
        }

        private void UpdateWithContent(int latestVersionId, ComplexProgressReporter progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log(string.Format("Updating application with content of version {0}", latestVersionId));

            var downloadContentPackageProgressReporter = new CustomProgressReporter<DownloadProgress>();
            var installProgressReporter = new ComplexProgressReporter();

            progressReporter.AddChild(downloadContentPackageProgressReporter, 1.0);
            progressReporter.AddChild(installProgressReporter, 0.25);

            downloadContentPackageProgressReporter.OnProgress += InvokeOnDownloadProgress;

            using (var temporaryDirectory = new TemporaryStorage(Path.Combine(_localAppData.TemporaryPath, string.Format("content-{0}-download", latestVersionId))))
            {
                string contentPackagePath = Path.Combine(temporaryDirectory.Path,
                    string.Format("content-{0}.package", latestVersionId));

                var contentSummary = _remoteAppData.GetContentSummary(latestVersionId);

                _remoteAppData.DownloadContentPackage(contentPackagePath, latestVersionId,
                    downloadContentPackageProgressReporter, cancellationToken);

                if (_localAppData.IsInstalled())
                {
                    Debug.Log("Removing previous application version data.");

                    _localAppData.Uninstall();
                }

                _localAppData.Install(contentPackagePath, contentSummary, latestVersionId,
                    installProgressReporter, cancellationToken);
            }
        }

        private void Process(CancellationToken cancellationToken)
        {
            State = PatcherState.Processing;

            InvokeOnProgress(0.0);
            InvokeOnDownloadProgress(new DownloadProgress
            {
                DownloadedBytes = 0,
                KilobytesPerSecond = 0,
                Progress = 0.0,
                TotalBytes = 0
            });

            var progressReporter = new ComplexProgressReporter();
            progressReporter.OnProgress += InvokeOnProgress;

            int? localVersionId = _localAppData.GetVersionId();
            Debug.Log(string.Format("Local version - {0}.", localVersionId.HasValue ? localVersionId.Value.ToString() : "none"));

            int latestVersionId = _remoteAppData.GetLatestVersionId();
            Debug.Log(string.Format("Latest version - {0}", latestVersionId));

            if (_configuration.ForceAppVersion != 0)
            {
                latestVersionId = _configuration.ForceAppVersion;
                Debug.Log(string.Format("Forcing application version - {0}", _configuration.ForceAppVersion));
            }

            if (localVersionId == null || localVersionId.Value != latestVersionId)
            {
                if (localVersionId != null && localVersionId.Value < latestVersionId &&
                    CanUpdateWithDiff(localVersionId.Value, latestVersionId))
                {
                    UpdateWithDiff(localVersionId.Value, latestVersionId, progressReporter, cancellationToken);
                }
                else
                {
                    UpdateWithContent(latestVersionId, progressReporter, cancellationToken);
                }
            }
            else
            {
                var contentSummary = _remoteAppData.GetContentSummary(latestVersionId);
                if (!_localAppData.CheckDataConsistency(contentSummary, latestVersionId))
                {
                    throw new Exception("Corrupted data.");
                }
            }
        }

        private void InvokeOnStateChanged(PatcherState state)
        {
            if (OnStateChanged != null)
            {
                Dispatcher.Invoke(() => OnStateChanged(state));
            }
        }

        private void InvokeOnProgress(double progress)
        {
            if (OnProgress != null)
            {
                Dispatcher.Invoke(() => OnProgress(progress));
            }
        }

        private void InvokeOnDownloadProgress(DownloadProgress progress)
        {
            if (OnDownloadProgress != null)
            {
                Dispatcher.Invoke(() => OnDownloadProgress(progress));
            }
        }

        void IDisposable.Dispose()
        {
            if (_thread != null && _thread.IsAlive)
            {
                _thread.Abort();
            }
        }
    }
}
