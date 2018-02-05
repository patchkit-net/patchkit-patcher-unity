using System;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class ContentRepairStrategy: IAppUpdaterStrategy
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(ContentRepairStrategy));

        private readonly AppUpdaterContext _context;

        public ContentRepairStrategy(AppUpdaterContext context)
        {
            Checks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _context = context;
        }

        public StrategyType GetStrategyType()
        {
            return StrategyType.ContentRepair;
        }

        public void Update(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Executing content repair strategy.");

            var commandFactory = new Commands.AppUpdaterCommandFactory();
            var geolocateCommand = commandFactory.CreateGeolocateCommand();

            geolocateCommand.Prepare(_context.StatusMonitor);
            geolocateCommand.Execute(cancellationToken);

            var installedVersionId = _context.App.GetInstalledVersionId();

            var checkVersionIntegrityCommand = commandFactory.CreateCheckVersionIntegrityCommand(installedVersionId, _context);
            var fdsa = commandFactory.
            DebugLogger.LogVariable(installedVersionId, "latestVersionId");

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_context.StatusMonitor);
            validateLicense.Execute(cancellationToken);
        }
    }
}