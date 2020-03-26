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
    public class AppUpdaterRepairAndDiffStrategy: IAppUpdaterStrategy
    {
        private readonly IAppUpdaterStrategy _repairStrategy;
        private readonly IAppUpdaterStrategy _diffStrategy;

        // not used
        public bool RepairOnError { get; set; }

        public AppUpdaterRepairAndDiffStrategy(AppUpdaterContext context, UpdaterStatus status)
        {
            Assert.IsNotNull(context, "Context is null");

            _repairStrategy = new AppUpdaterRepairStrategy(context, status);
            _diffStrategy = new AppUpdaterDiffStrategy(context, status);
        }

        public StrategyType GetStrategyType()
        {
            return StrategyType.RepairAndDiff;
        }

        public void Update(CancellationToken cancellationToken)
        {
            _repairStrategy.Update(cancellationToken);
            _diffStrategy.Update(cancellationToken);
        }
    }
}