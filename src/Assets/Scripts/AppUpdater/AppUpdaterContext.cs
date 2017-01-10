using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Patcher.UI.Dialogs;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    internal class AppUpdaterContext
    {
        public readonly AppData.AppData Data;

        public readonly AppUpdaterConfiguration Configuration;

        public readonly IStatusMonitor StatusMonitor;

        public readonly ILicenseDialog LicenseDialog;

        public AppUpdaterContext(string appSecret, string appDataPath, AppUpdaterConfiguration configuration) :
            this(new AppData.AppData(appSecret, appDataPath), configuration, new StatusMonitor(), FindLicenseDialog())
        {
        }

        public AppUpdaterContext(AppData.AppData data, AppUpdaterConfiguration configuration, IStatusMonitor statusMonitor, ILicenseDialog licenseDialog)
        {
            AssertChecks.ArgumentNotNull(data, "data");
            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");
            AssertChecks.ArgumentNotNull(licenseDialog, "licenseDialog");

            Data = data;
            Configuration = configuration;
            StatusMonitor = statusMonitor;
            LicenseDialog = licenseDialog;
        }

        private static LicenseDialog FindLicenseDialog()
        {
            return Object.FindObjectOfType<LicenseDialog>();
        }
    }
}