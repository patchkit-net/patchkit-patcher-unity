using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Patcher.UI.Dialogs;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterContext
    {
        public readonly AppData.AppData Data;

        public readonly AppUpdaterConfiguration Configuration;

        public readonly IStatusMonitor StatusMonitor;

        public readonly ILicenseDialog LicenseDialog;

        public AppUpdaterContext(ILocalData localData, IRemoteData remoteData, AppUpdaterConfiguration configuration) :
            this(localData, remoteData, configuration, new StatusMonitor(), UI.Dialogs.LicenseDialog.Instance)
        {
        }

        public AppUpdaterContext(ILocalData localData, IRemoteData remoteData, AppUpdaterConfiguration configuration, IStatusMonitor statusMonitor, ILicenseDialog licenseDialog)
        {
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(remoteData, "remoteData");
            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");
            AssertChecks.ArgumentNotNull(licenseDialog, "licenseDialog");

            Data = new AppData.AppData(localData, remoteData);
            Configuration = configuration;
            StatusMonitor = statusMonitor;
            LicenseDialog = licenseDialog;
        }
    }
}