using JetBrains.Annotations;

public partial class Patcher
{
    public async void OnStartAppRequested()
    {
        await SafeInvoke(func: StartApp);
    }

    public async void OnUpdateAppRequested()
    {
        await SafeInvoke(func: UpdateApp);
    }

    public async void OnUpdateAppWithLicenseKeyRequested(
        [NotNull] string licenseKey)
    {
        await SafeInvoke(
            func: () => UpdateAppWithLicenseKey(licenseKey: licenseKey));
    }

    public void OnCancelUpdateAppRequested()
    {
        CancelUpdateApp();
    }

    public void OnQuitRequested()
    {
        Quit();
    }

    public async void OnAcceptErrorRequested()
    {
        await AcceptError();
    }
}