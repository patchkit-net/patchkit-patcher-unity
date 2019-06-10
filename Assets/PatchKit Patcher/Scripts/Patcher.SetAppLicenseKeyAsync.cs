using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

public partial class Patcher
{
#pragma warning disable 1998
    private async Task<bool> SetAppLicenseKeyAsync([NotNull] string licenseKey)
#pragma warning restore 1998
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