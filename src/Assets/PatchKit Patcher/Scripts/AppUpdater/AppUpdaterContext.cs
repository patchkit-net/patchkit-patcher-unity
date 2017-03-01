using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Patcher.UI.Dialogs;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterContext
    {
        public readonly App App;

        public readonly AppUpdaterConfiguration Configuration;

        public readonly IStatusMonitor StatusMonitor;

        public readonly ILicenseDialog LicenseDialog;

        public AppUpdaterContext(App app, AppUpdaterConfiguration configuration) :
            this(app, configuration, new StatusMonitor(), UI.Dialogs.LicenseDialog.Instance)
        {
        }

        public AppUpdaterContext(App app, AppUpdaterConfiguration configuration, IStatusMonitor statusMonitor, ILicenseDialog licenseDialog)
        {
            AssertChecks.ArgumentNotNull(app, "app");
            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");
            AssertChecks.ArgumentNotNull(licenseDialog, "licenseDialog");

            App = app;
            Configuration = configuration;
            StatusMonitor = statusMonitor;
            LicenseDialog = licenseDialog;
        }
    }
}