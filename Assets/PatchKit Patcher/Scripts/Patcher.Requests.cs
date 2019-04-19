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

    public async void OnCancelUpdateAppRequested()
    {
        if (State.Kind != PatcherStateKind.UpdatingApp &&
            State.Kind != PatcherStateKind.AskingForAppLicenseKey)
        {
            return;
        }

        await SafeInvoke(func: CancelUpdateApp2);
    }

    public async void OnSetLicenseKeyAndUpdateAppRequested(
        [NotNull] string licenseKey)
    {
        if (State.Kind != PatcherStateKind.AskingForAppLicenseKey)
        {
            return;
        }

        await SafeInvoke(func: () => SetLicenseKey(licenseKey: licenseKey));

        await UpdateApp();
    }

    public async void OnCancelSettingLicenseKeyRequested()
    {
        if (State.Kind != PatcherStateKind.AskingForAppLicenseKey)
        {
            return;
        }

        await CancelSettingLicenseKey();
    }

    //TODO: Remove after moving to Legacy
    public void Quit()
    {
        OnQuitRequested();
    }

    //TODO: Remove after moving to Legacy
    public void CancelUpdateApp()
    {
        OnCancelUpdateAppRequested();
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