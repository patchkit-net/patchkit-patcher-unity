using System;
using System.Linq;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using FileIntegrityStatus = PatchKit.Unity.Patcher.AppUpdater.Commands.FileIntegrityStatus;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class ContentRepairStrategy: IAppUpdaterStrategy
    {
        private readonly AppUpdaterContext _context;

        private readonly ILogger _logger;

        public ContentRepairStrategy(AppUpdaterContext context)
        {
            Assert.IsNotNull(context, "Context is null");

            _context = context;

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

            var commandFactory = new Commands.AppUpdaterCommandFactory();

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_context.StatusMonitor);
            validateLicense.Execute(cancellationToken);

            var geolocateCommand = commandFactory.CreateGeolocateCommand();
            geolocateCommand.Prepare(_context.StatusMonitor);
            geolocateCommand.Execute(cancellationToken);

            var resource = _context.App.RemoteData.GetContentPackageResource(
                installedVersionId, 
                validateLicense.KeySecret, 
                geolocateCommand.CountryCode);

            var checkVersionIntegrityCommand = commandFactory.CreateCheckVersionIntegrityCommand(installedVersionId, _context);
            checkVersionIntegrityCommand.Prepare(_context.StatusMonitor);
            checkVersionIntegrityCommand.Execute(cancellationToken);

            if (!resource.HasMetaUrls())
            {
                throw new ArgumentException("Cannot execute content repair strategy without meta files.");
            }

            _logger.LogDebug("Downloading the meta file.");
            var downloader = new HttpDownloader(metaDestination, resource.GetMetaUrls());
            downloader.Download(cancellationToken);

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

            repairCommand.Prepare(_context.StatusMonitor);
            repairCommand.Execute(cancellationToken);
        }
    }
}