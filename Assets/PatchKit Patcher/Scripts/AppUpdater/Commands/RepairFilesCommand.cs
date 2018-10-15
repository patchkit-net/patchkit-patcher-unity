using System;
using System.Collections.Generic;
using System.IO;
using PatchKit.Logging;
using PatchKit.Network;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IRepairFilesCommand : IAppUpdaterCommand
    {
    }

    public class RepairFilesCommand : BaseAppUpdaterCommand, IRepairFilesCommand
    {
        private struct EntryStatus
        {
            public OperationStatus RepairStatus;
            public DownloadStatus DownloadStatus;
        }

        private RemoteResource _resource;
        private Pack1Meta _meta;
        private Pack1Meta.FileEntry[] _entries;

        private ILocalDirectory _localData;

        private string _packagePath;
        private string _packagePassword;

        private const string _unpackingSuffix = "_";

        private readonly ILogger _logger;

        private readonly Dictionary<Pack1Meta.FileEntry, EntryStatus> _entryStatus
            = new Dictionary<Pack1Meta.FileEntry, EntryStatus>();

        public RepairFilesCommand(
            RemoteResource resource,
            Pack1Meta meta,
            Pack1Meta.FileEntry[] fileEntries,
            string destinationPackagePath,
            string packagePassword,
            ILocalDirectory localData)
        {
            _resource = resource;
            _meta = meta;
            _entries = fileEntries;

            _packagePath = destinationPackagePath;
            _packagePassword = packagePassword;

            _localData = localData;

            _logger = PatcherLogManager.DefaultLogger;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            foreach (var entry in _entries)
            {
                var tempDirName = _packagePath + string.Format("{0}_{1}_{2}", entry.Name, entry.Offset, entry.Size);
                TemporaryDirectory.ExecuteIn(tempDirName, (tempDir) =>
                {
                    _logger.LogDebug(string.Format("Repairing the file {0}", entry.Name));
                    string packagePath = Path.Combine(tempDir.Path, ".pack" + Path.GetRandomFileName());
                    string unarchivePath = Path.Combine(tempDir.Path, Path.GetRandomFileName());

                    if (!Directory.Exists(unarchivePath))
                    {
                        DirectoryOperations.CreateDirectory(unarchivePath);
                    }

                    var downloader = new ChunkedHttpDownloader(packagePath, _resource.ResourceUrls, _resource.ChunksData, _resource.Size);

                    long start = entry.Offset.GetValueOrDefault();
                    long end = (start + entry.Size.GetValueOrDefault()) - 1; // Offset by 1 to denote a byte index

                    var range = new BytesRange(start, end);

                    downloader.SetRange(range);
                    var effectiveRange  = downloader.CalculateContainingChunksRange(range);

                    long totalData = effectiveRange.End == -1 ? _resource.Size - effectiveRange.Start : effectiveRange.End - effectiveRange.Start;

                    var downloadStatus = _entryStatus[entry].DownloadStatus;
                    var repairStatus = _entryStatus[entry].RepairStatus;

                    downloadStatus.IsActive.Value = true;
                    downloadStatus.TotalBytes.Value = totalData;
                    downloadStatus.Description.Value = "Downloading broken file...";
                    downloadStatus.Bytes.Value = 0;

                    downloader.DownloadProgressChanged += downloadedBytes =>
                    {
                        downloadStatus.Bytes.Value = downloadedBytes;
                    };

                    _logger.LogDebug(string.Format("Downloading the partial package with range {0}-{1}", start, end));
                    downloader.Download(cancellationToken);

                    downloadStatus.IsActive.Value = false;

                    repairStatus.IsActive.Value = true;
                    repairStatus.Description.Value = "Reparing broken file...";
                    repairStatus.Progress.Value = 0.0;

                    _logger.LogDebug("Unarchiving the package.");
                    var unarchiver = new Pack1Unarchiver(packagePath, _meta, unarchivePath, _packagePassword, _unpackingSuffix, effectiveRange);
                    unarchiver.UnarchiveProgressChanged += (name, isFile, unarchiveEntry, amount,  entryProgress) =>
                    {
                        repairStatus.Progress.Value = entryProgress;
                    };

                    unarchiver.UnarchiveSingleFile(entry, cancellationToken);

                    EmplaceFile(Path.Combine(unarchivePath, entry.Name + _unpackingSuffix), Path.Combine(_localData.Path, entry.Name));

                    repairStatus.IsActive.Value = false;
                });
            }
        }

        public override void Prepare(UpdaterStatus status)
        {
            base.Prepare(status);

            foreach(var entry in _entries)
            {
                var repairStatus = new OperationStatus
                {
                    Weight = { Value = StatusWeightHelper.GetUnarchivePackageWeight(entry.Size.Value) }
                };
                status.RegisterOperation(repairStatus);

                var downloadStatus = new DownloadStatus
                {
                    Weight = {Value = StatusWeightHelper.GetDownloadWeight(entry.Size.Value)}
                };
                status.RegisterOperation(downloadStatus);

                _entryStatus[entry] = new EntryStatus
                {
                    RepairStatus = repairStatus,
                    DownloadStatus = downloadStatus
                };
            }

            _localData.PrepareForWriting();
        }

        private void EmplaceFile(string source, string target)
        {
            _logger.LogDebug(string.Format("Installing file {0} into {1}", source, target));

            if (!File.Exists(source))
            {
                throw new Exception(string.Format("Source file {0} doesn't exist.", source));
            }

            DirectoryOperations.CreateParentDirectory(target);

            if (File.Exists(target))
            {
                FileOperations.Delete(target);
            }

            FileOperations.Move(source, target);
        }
    }
}