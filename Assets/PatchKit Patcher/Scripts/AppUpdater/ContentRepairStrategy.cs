using System;
using System.Linq;
using System.Collections.Generic;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using FileIntegrityStatus = PatchKit.Unity.Patcher.AppUpdater.Commands.FileIntegrityStatus;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class ContentRepairStrategy: IAppUpdaterStrategy
    {
        private readonly AppUpdaterContext _context;

        private readonly UpdaterStatus _status;

        private readonly ILogger _logger;

        public ContentRepairStrategy(AppUpdaterContext context, UpdaterStatus status)
        {
            Assert.IsNotNull(context, "Context is null");

            _context = context;
            _status = status;

            _logger = PatcherLogManager.DefaultLogger;
        }

        public StrategyType GetStrategyType()
        {
            return StrategyType.ContentRepair;
        }

        public void Update(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Executing content repair strategy.");
            var installedVersionId = _context.App.GetInstalledVersionId();

            string metaDestination = _context.App.DownloadDirectory.GetDiffPackageMetaPath(installedVersionId);

            var commandFactory = new AppUpdaterCommandFactory();

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_status);
            validateLicense.Execute(cancellationToken);

            var geolocateCommand = commandFactory.CreateGeolocateCommand();
            geolocateCommand.Prepare(_status);
            geolocateCommand.Execute(cancellationToken);

            var resource = _context.App.RemoteData.GetContentPackageResource(
                installedVersionId, 
                validateLicense.KeySecret, 
                geolocateCommand.CountryCode);

            if (!resource.HasMetaUrls())
            {
                throw new ArgumentException("Cannot execute content repair strategy without meta files.");
            }

            _logger.LogDebug("Downloading the meta file.");
            var downloader = new HttpDownloader(metaDestination, resource.GetMetaUrls());
            downloader.Download(cancellationToken);

            var checkVersionIntegrityCommand = commandFactory.CreateCheckVersionIntegrityCommand(installedVersionId, _context);
            checkVersionIntegrityCommand.Prepare(_status);
            checkVersionIntegrityCommand.Execute(cancellationToken);

            var meta = Pack1Meta.ParseFromFile(metaDestination);

            var filesIntegrity = checkVersionIntegrityCommand.Results.Files;

            var brokenFiles = filesIntegrity
                // Filter only files with invalid size or hash
                .Where(f => f.Status == FileIntegrityStatus.InvalidHash || f.Status == FileIntegrityStatus.InvalidSize)
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
            
            var repairCommand = commandFactory.CreateRepairFilesCommand(
                installedVersionId,
                _context,
                resource,
                brokenFiles,
                meta);

            repairCommand.Prepare(_status);
            repairCommand.Execute(cancellationToken);

            _logger.LogDebug("Repair successful, following up with a diff.");
            PerformDiff(cancellationToken);
        }

#region DIFF_COPY_PASTE
        private struct DiffCommands
        {
            public IDownloadPackageCommand Download;
            public IInstallDiffCommand Install;
        }

        private void PerformDiff(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Updating with diff strategy.");

            var latestVersionId = _context.App.GetLatestVersionId();
            var currentLocalVersionId = _context.App.GetInstalledVersionId();

            var commandFactory = new AppUpdaterCommandFactory();
            var geolocateCommand = commandFactory.CreateGeolocateCommand();

            geolocateCommand.Prepare(_status);
            geolocateCommand.Execute(cancellationToken);

            var checkDiskSpaceCommand = commandFactory.CreateCheckDiskSpaceCommandForDiff(latestVersionId, _context);
            checkDiskSpaceCommand.Prepare(_status);
            checkDiskSpaceCommand.Execute(cancellationToken);

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_status);
            validateLicense.Execute(cancellationToken);

            var diffCommandsList = new List<DiffCommands>();

            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                DiffCommands diffCommands;

                diffCommands.Download = commandFactory.CreateDownloadDiffPackageCommand(i, validateLicense.KeySecret,
                    geolocateCommand.CountryCode, _context);
                diffCommands.Download.Prepare(_status);

                diffCommands.Install = commandFactory.CreateInstallDiffCommand(i, _context);
                diffCommands.Install.Prepare(_status);

                diffCommandsList.Add(diffCommands);
            }

            foreach (var diffCommands in diffCommandsList)
            {
                diffCommands.Download.Execute(cancellationToken);
                diffCommands.Install.Execute(cancellationToken);
            }

            _context.App.DownloadDirectory.Clear();
        }
#endregion
    }
}