using System.Threading.Tasks;
using JetBrains.Annotations;

public partial class Patcher
{
    private async Task<bool> SetAppLicenseKeyAndUpdateAppAsync(
        [NotNull] string licenseKey)
    {
        if (!await SetAppLicenseKeyAsync(licenseKey: licenseKey))
        {
            return false;
        }

        return await UpdateAppAsync();
    }
}