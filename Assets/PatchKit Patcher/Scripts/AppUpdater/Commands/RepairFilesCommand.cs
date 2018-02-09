using System;
using System.IO;
using System.Linq;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IRepairFilesCommand : IAppUpdaterCommand
    {
    }

    public class RepairFilesCommand : BaseAppUpdaterCommand, IRepairFilesCommand
    {
        private RemoteResource _resource;
        private Pack1Meta _meta;
        private Pack1Meta.FileEntry[] _entries;

        private ILocalDirectory _localData;

        private string _packagePath;
        private string _packagePassword;

        private IGeneralStatusReporter _statusReporter;
        private IDownloadStatusReporter _downloadStatusReporter;

        private readonly ILogger _logger;

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
                using (var tempDir = new TemporaryDirectory(_packagePath + string.Format("{0}_{1}_{2}", entry.Name, entry.Offset, entry.Size)))
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
                    long end = start + entry.Size.GetValueOrDefault();

                    var range = new Network.BytesRange(start, end);

                    downloader.SetRange(range);
                    var effectiveRange  = downloader.CalculateContainingChunksRange(range);

                    long totalData = effectiveRange.End == -1 ? _resource.Size - effectiveRange.Start : effectiveRange.End - effectiveRange.Start;

                    downloader.DownloadProgressChanged += downloadedBytes => {
                        _downloadStatusReporter.OnDownloadProgressChanged(downloadedBytes, totalData);
                    };

                    _logger.LogDebug(string.Format("Downloading the partial package with range {0}-{1}", start, end));
                    downloader.Download(cancellationToken);

                    _logger.LogDebug("Unarchiving the package.");
                    var unarchiver = new Pack1Unarchiver(packagePath, _meta, unarchivePath, _packagePassword, "", effectiveRange);
                    unarchiver.UnarchiveProgressChanged += (name, isFile, unarchiveEntry, amount,  entryProgress) => {
                        _statusReporter.OnProgressChanged(entryProgress, string.Format("Unarchiving {0}", name));
                    };

                    unarchiver.UnarchiveSingleFile(entry, cancellationToken);

                    EmplaceFile(Path.Combine(unarchivePath, entry.Name), Path.Combine(_localData.Path, entry.Name));
                }
            }
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            double weight = StatusWeightHelper.GetRepairFilesWeight(_entries);
            _statusReporter = statusMonitor.CreateGeneralStatusReporter(weight);

            _downloadStatusReporter = statusMonitor.CreateDownloadStatusReporter(weight);

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