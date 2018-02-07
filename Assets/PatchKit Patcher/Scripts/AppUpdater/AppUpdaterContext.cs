using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.UI.Dialogs;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterContext
    {
        public readonly App App;

        public readonly AppUpdaterConfiguration Configuration;

        public readonly ILicenseDialog LicenseDialog;

        public AppUpdaterContext(App app, AppUpdaterConfiguration configuration) :
            this(app, configuration, UI.Dialogs.LicenseDialog.Instance)
        {
        }

        public AppUpdaterContext(App app, AppUpdaterConfiguration configuration, ILicenseDialog licenseDialog)
        {
            Checks.ArgumentNotNull(app, "app");
            Checks.ArgumentNotNull(licenseDialog, "licenseDialog");

            App = app;
            Configuration = configuration;
            LicenseDialog = licenseDialog;
        }
    }
}