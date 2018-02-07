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
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(ContentRepairStrategy));

        private readonly AppUpdaterContext _context;

        private readonly ILogger _logger;

        public ContentRepairStrategy(AppUpdaterContext context)
        {
            Checks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

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
                .Where(f => f.Status != FileIntegrityStatus.Ok)
                .Select(integrity => meta.Files.Single(file => file.Name == integrity.FileName))
                .Where(file => file.Type == Pack1Meta.RegularFileType)
                .ToArray();

            _logger.LogDebug(string.Format("Broken files count: {0}", brokenFiles.Length));

            // TODO: Move to command factory
            var packagePath = _context.App.DownloadDirectory.GetContentPackagePath(installedVersionId);
            var packagePassword = _context.App.RemoteData.GetContentPackageResourcePassword(installedVersionId);

            var repairCommand = new RepairFilesCommand(
                resource, 
                meta, 
                brokenFiles,
                packagePath,
                packagePassword,
                _context.App.LocalDirectory);

            repairCommand.Prepare(_context.StatusMonitor);
            repairCommand.Execute(cancellationToken);
        }
    }
}