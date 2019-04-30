using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    [NotNull]
    private Task CancelSettingLicenseKey()
    {
        Debug.Log(message: "Cancelling setting license key...");

        Assert.IsTrue(
            condition: State.Kind == PatcherStateKind.AskingForAppLicenseKey);

        ModifyState(x: () => { State.Kind = PatcherStateKind.Idle; });

        Debug.Log(message: "Successfully cancelled license key.");

        return Task.CompletedTask;
    }

    [NotNull]
    private async Task SetLicenseKeyAndUpdateApp([NotNull] string licenseKey)
    {
        Debug.Log(message: $"Setting license key to '{licenseKey}'...");

        Assert.IsNotNull(value: State.AppState);
        Assert.IsTrue(
            condition: State.Kind == PatcherStateKind.AskingForAppLicenseKey);

        ModifyState(
            x: () =>
            {
                State.AppState.LicenseKey = licenseKey;
                State.Kind = PatcherStateKind.Idle;
            });

        Debug.Log(message: "Successfully set license key.");

        await UpdateApp();
    }
}