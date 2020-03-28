using System;
using System.Linq;
using System.Collections.Generic;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using FileIntegrityStatus = PatchKit.Unity.Patcher.AppUpdater.Commands.FileIntegrityStatus;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterRepairStrategy: IAppUpdaterStrategy
    {
        private readonly AppUpdaterContext _context;

        private readonly UpdaterStatus _status;

        private readonly ILogger _logger;

        // not used
        public bool RepairOnError { get; set; }

        public AppUpdaterRepairStrategy(AppUpdaterContext context, UpdaterStatus status)
        {
            Assert.IsNotNull(context, "Context is null");

            _context = context;
            _status = status;

            _logger = PatcherLogManager.DefaultLogger;
        }

        public StrategyType GetStrategyType()
        {
            return StrategyType.Repair;
        }

        public void Update(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Executing content repair strategy.");
            var installedVersionId = _context.App.GetInstalledVersionId();

            string metaDestination = _context.App.DownloadDirectory.GetDiffPackageMetaPath(installedVersionId);

            var commandFactory = new AppUpdaterCommandFactory();

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_status, cancellationToken);
            validateLicense.Execute(cancellationToken);

            var geolocateCommand = commandFactory.CreateGeolocateCommand();
            geolocateCommand.Prepare(_status, cancellationToken);
            geolocateCommand.Execute(cancellationToken);

            var resource = _context.App.RemoteData.GetContentPackageResource(
                installedVersionId,
                validateLicense.KeySecret,
                geolocateCommand.CountryCode,
                cancellationToken);

            if (!resource.HasMetaUrls())
            {
                throw new ArgumentException("Cannot execute content repair strategy without meta files.");
            }

            _logger.LogDebug("Downloading the meta file.");
            var downloader = new HttpDownloader(metaDestination, resource.GetMetaUrls());
            downloader.Download(cancellationToken);

            ICheckVersionIntegrityCommand checkVersionIntegrityCommand
                = commandFactory.CreateCheckVersionIntegrityCommand(installedVersionId, _context, true, true, cancellationToken);

            checkVersionIntegrityCommand.Prepare(_status, cancellationToken);
            checkVersionIntegrityCommand.Execute(cancellationToken);

            var meta = Pack1Meta.ParseFromFile(metaDestination);

            FileIntegrity[] filesIntegrity = checkVersionIntegrityCommand.Results.Files;

            var contentSummary = _context.App.RemoteMetaData.GetContentSummary(installedVersionId, cancellationToken);

            foreach (var invalidVersionIdFile in filesIntegrity.Where(x =>
                x.Status == FileIntegrityStatus.InvalidVersion).ToArray())
            {
                var fileName = invalidVersionIdFile.FileName;
                var file = contentSummary.Files.First(x => x.Path == fileName);

                var localPath = _context.App.LocalDirectory.Path.PathCombine(file.Path);

                string actualFileHash = HashCalculator.ComputeFileHash(localPath);
                if (actualFileHash != file.Hash)
                {
                    FileOperations.Delete(localPath, cancellationToken);
                    _context.App.LocalMetaData.RegisterEntry(fileName, installedVersionId);
                    invalidVersionIdFile.Status = FileIntegrityStatus.MissingData;
                }
                else
                {
                    _context.App.LocalMetaData.RegisterEntry(fileName, installedVersionId);
                    invalidVersionIdFile.Status = FileIntegrityStatus.Ok;
                }
            }

            Pack1Meta.FileEntry[] brokenFiles = filesIntegrity
                // Filter only files with invalid size, hash or missing entirely
                .Where(f => f.Status == FileIntegrityStatus.InvalidHash
                         || f.Status == FileIntegrityStatus.InvalidSize
                         || f.Status == FileIntegrityStatus.MissingData)
                // Map to file entires from meta
                .Select(integrity => meta.Files.SingleOrDefault(file => file.Name == integrity.FileName))
                // Filter only regular files
                .Where(file => file.Type == Pack1Meta.RegularFileType)
                .ToArray();

            if (brokenFiles.Length == 0)
            {
                _logger.LogDebug("Nothing to repair.");
                return;
            }
            _logger.LogDebug(string.Format("Broken files count: {0}", brokenFiles.Length));

            IRepairFilesCommand repairCommand = commandFactory.CreateRepairFilesCommand(
                installedVersionId,
                _context,
                resource,
                brokenFiles,
                meta);

            repairCommand.Prepare(_status, cancellationToken);
            repairCommand.Execute(cancellationToken);
        }
    }
}