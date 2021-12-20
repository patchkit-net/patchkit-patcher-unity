using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class CheckVersionIntegrityCommand : BaseAppUpdaterCommand, ICheckVersionIntegrityCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CheckVersionIntegrityCommand));

        private const int MaxThreadCount = 16;
        private readonly int _versionId;
        private readonly AppContentSummary _versionSummary;
        private readonly ILocalDirectory _localDirectory;
        private readonly ILocalMetaData _localMetaData;

        private OperationStatus _status;
        private volatile bool _isCheckingHash;
        private volatile bool _isCheckingSize;
        private volatile int _currentThreadCount;

        public CheckVersionIntegrityCommand(int versionId, AppContentSummary versionSummary,
            ILocalDirectory localDirectory, ILocalMetaData localMetaData, bool isCheckingHash, bool isCheckingSize)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            Checks.ArgumentNotNull(versionSummary, "versionSummary");
            Checks.ArgumentNotNull(localDirectory, "localDirectory");
            Checks.ArgumentNotNull(localMetaData, "localMetaData");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(versionId, "versionId");

            _versionId = versionId;
            _versionSummary = versionSummary;
            _localDirectory = localDirectory;
            _localMetaData = localMetaData;
            _isCheckingSize = isCheckingSize;
            _isCheckingHash = isCheckingHash;
        }
        
        public CheckVersionIntegrityCommand(string[] brokenFiles)
        {
            Results = new VersionIntegrity(brokenFiles.Select(file =>
                new FileIntegrity(file, FileIntegrityStatus.MissingData)).ToArray());
        }

        public override void Prepare(UpdaterStatus status, CancellationToken cancellationToken)
        {
            base.Prepare(status, cancellationToken);

            Checks.ArgumentNotNull(status, "statusMonitor");

            DebugLogger.Log("Preparing version integrity check.");

            _status = new OperationStatus
            {
                Weight = {Value = StatusWeightHelper.GetCheckVersionIntegrityWeight(_versionSummary)},
                Description = {Value = "Checking version integrity..."}
            };
            status.RegisterOperation(_status);
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            var integrityCheckStopwatch = new Stopwatch();
            var optionalParams = new PatcherStatistics.OptionalParams
            {
                VersionId = _versionId,
            };

            System.Func<PatcherStatistics.OptionalParams> timedParams = () => new PatcherStatistics.OptionalParams {
                VersionId = optionalParams.VersionId,
                Time = integrityCheckStopwatch.Elapsed.Seconds,
            };

            try
            {
                PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.ValidationStarted, optionalParams);
                ExecuteInternal(cancellationToken);

                if (Results.Files.All(integrity => integrity.Status == FileIntegrityStatus.Ok))
                {
                    PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.ValidationSucceeded, timedParams());
                }
                else
                {
                    PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.ValidationFailed, timedParams());
                }
            }
            catch (System.OperationCanceledException)
            {
                PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.ValidationCanceled, timedParams());
                throw;
            }
        }

        private static volatile FileIntegrity[] files;

        private void ExecuteInternal(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Checking version integrity.");

            _status.IsActive.Value = true;

            files = new FileIntegrity[_versionSummary.Files.Length];
            int fileIndex = 0;
            foreach (var file in _versionSummary.Files)
            {
                while (true)
                {
                    int tmp;
                    lock (this)
                    {
                        tmp = _currentThreadCount;
                    }

                    if (tmp < MaxThreadCount)
                    {
                        break;
                    }

                    Thread.Sleep(1);
                }

                lock (this)
                {
                    _currentThreadCount++;
                }

                var fileInThread = file;
                var threadFileIndex = fileIndex;
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        var fileIntegrity = CheckFile(fileInThread);
                        lock (this)
                        {
                            files[threadFileIndex] = fileIntegrity;
                        }
                    }
                    catch (Exception e)
                    {
                        DebugLogger.LogError(e.ToString());
                        throw;
                    }
                    finally
                    {
                        lock (this)
                        {
                            _currentThreadCount--;
                        }
                    }
                });

                _status.Progress.Value = (fileIndex + 1) / (double) _versionSummary.Files.Length;
                fileIndex++;
            }

            while (true)
            {
                lock (this)
                {
                    if (_currentThreadCount == 0)
                    {
                        break;
                    }
                }

                Thread.Sleep(1000);
            }

            Results = new VersionIntegrity(files);

            _status.IsActive.Value = false;
        }

        private int _eventSendCount;
        private FileIntegrity CheckFile(AppContentSummaryFile file)
        {
            Action onVerificationFailed = () =>
            {
                _eventSendCount++;
                if (_eventSendCount > 32)
                {
                    return;
                }
                PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.FileVerificationFailed, new PatcherStatistics.OptionalParams
                {
                    FileName = file.Path,
                    Size = file.Size,
                });
            };

            string localPath = _localDirectory.Path.PathCombine(file.Path);
            if (!File.Exists(localPath))
            {
                DebugLogger.LogWarning("File " + localPath + " not exist");
                onVerificationFailed();
                return new FileIntegrity(file.Path, FileIntegrityStatus.MissingData);
            }

            if (!_localMetaData.IsEntryRegistered(file.Path))
            {
                DebugLogger.LogWarning("File " + file.Path + " is not entry registered");
                onVerificationFailed();
                return new FileIntegrity(file.Path, FileIntegrityStatus.MissingMetaData);
            }

            int actualVersionId = _localMetaData.GetEntryVersionId(file.Path);
            if (actualVersionId != _versionId)
            {
                DebugLogger.LogWarning("File " + file.Path + " version is " + actualVersionId + ", expected " + _versionId);
                onVerificationFailed();
                return FileIntegrity.InvalidVersion(_versionId, actualVersionId, file.Path);
            }

            if (_isCheckingSize)
            {
                long actualSize = new FileInfo(localPath).Length;
                if (actualSize != file.Size)
                {
                    DebugLogger.LogWarning("File " + localPath + " size is " + actualSize + ", expected " + file.Size);
                    onVerificationFailed();
                    return FileIntegrity.InvalidSize(file.Size, actualSize, file.Path);
                }
            }

            if (_isCheckingHash)
            {
                string actualFileHash = HashCalculator.ComputeFileHash(localPath);
                if (actualFileHash != file.Hash)
                {
                    DebugLogger.LogWarning("File " + localPath + " hash is " + actualFileHash + ", expected " + file.Hash);

                    onVerificationFailed();
                    return FileIntegrity.InvalidHash(file.Hash, actualFileHash, file.Path);
                }
            }

            return new FileIntegrity(file.Path, FileIntegrityStatus.Ok);
        }

        public VersionIntegrity Results { get; private set; }
    }
}