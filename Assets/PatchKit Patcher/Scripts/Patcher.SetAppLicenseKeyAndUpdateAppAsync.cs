using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task<bool> SetAppLicenseKeyAndUpdateAppAsync(
        [NotNull] string licenseKey)
    {
        if (!await SetAppLicenseKey(licenseKey: licenseKey))
        {
            return false;
        }

        return await UpdateApp();
    }
}