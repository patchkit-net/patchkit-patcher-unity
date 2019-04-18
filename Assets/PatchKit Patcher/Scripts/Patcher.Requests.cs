using JetBrains.Annotations;

public partial class Patcher
{
    public async void OnStartAppRequested()
    {
        if (State.Kind != PatcherStateKind.Idle)
        {
            return;
        }

        await SafeInvoke(func: StartApp);
    }

    public async void OnUpdateAppRequested()
    {
        if (State.Kind != PatcherStateKind.Idle)
        {
            return;
        }

        await SafeInvoke(func: UpdateApp);
    }

    public async void OnUpdateAppWithLicenseKeyRequested(
        [NotNull] string licenseKey)
    {
        if (State.Kind != PatcherStateKind.AskingForAppLicenseKey)
        {
            return;
        }

        await SafeInvoke(
            func: () => UpdateAppWithLicenseKey(licenseKey: licenseKey));
    }

    public void OnCancelUpdateAppRequested()
    {
        if (State.Kind != PatcherStateKind.UpdatingApp &&
            State.Kind != PatcherStateKind.AskingForAppLicenseKey)
        {
            return;
        }

        CancelUpdateApp();
    }

    //TODO: Make it private after moving to Legacy
    public void Quit()
    {
        OnQuitRequested();
    }

    public void OnQuitRequested()
    {
        Quit2();
    }

    public async void OnAcceptErrorRequested()
    {
        if (State.Kind != PatcherStateKind.DisplayingError)
        {
            return;
        }

        await AcceptError();
    }
}