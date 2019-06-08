using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

public partial class Patcher
{
    private async Task<bool> SetAppLicenseKeyAsync([NotNull] string licenseKey)
    {
        if (!CanPerformNewForegroundTask() ||
            !_hasApp)
        {
            return false;
        }

        Debug.Log(message: $"Setting license key to '{licenseKey}'...");

        _appLicenseKey = licenseKey;
        SendStateChanged();

        Debug.Log(message: "Successfully set license key.");

        return true;
    }
}