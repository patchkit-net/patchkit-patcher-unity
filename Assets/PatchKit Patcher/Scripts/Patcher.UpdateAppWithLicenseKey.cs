using System.Threading.Tasks;
using JetBrains.Annotations;

public partial class Patcher
{
    private async Task UpdateAppWithLicenseKey([NotNull] string licenseKey)
    {
        if (State.Kind != PatcherStateKind.AskingForLicenseKey)
        {
            return;
        }

        ModifyState(x: () => State.AppState.LicenseKey = licenseKey);

        await UpdateApp();
    }
}