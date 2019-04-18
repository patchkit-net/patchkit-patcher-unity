using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task UpdateAppWithLicenseKey([NotNull] string licenseKey)
    {
        Assert.IsNotNull(value: State.AppState);

        ModifyState(x: () => State.AppState.LicenseKey = licenseKey);

        await UpdateApp();
    }
}