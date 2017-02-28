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
            Checks.ArgumentNotNull(app, "app");
            Checks.ArgumentNotNull(statusMonitor, "statusMonitor");
            Checks.ArgumentNotNull(licenseDialog, "licenseDialog");

            App = app;
            Configuration = configuration;
            StatusMonitor = statusMonitor;
            LicenseDialog = licenseDialog;
        }
    }
}