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

    public async void OnCancelUpdateAppRequested()
    {
        if (State.Kind != PatcherStateKind.UpdatingApp &&
            State.Kind != PatcherStateKind.AskingForAppLicenseKey)
        {
            return;
        }

        await SafeInvoke(func: CancelUpdateApp);
    }

    //TODO: Make it private after moving to Legacy
    public void Quit()
    {
        OnQuitRequested();
    }

    public async void OnQuitRequested()
    {
        await SafeInvoke(func: Quit2);
    }

    public async void OnAcceptErrorRequested()
    {
        if (State.Kind != PatcherStateKind.DisplayingError)
        {
            return;
        }

        await SafeInvoke(func: AcceptError);
    }
}