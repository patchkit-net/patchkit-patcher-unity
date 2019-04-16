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

    public void OnCancelUpdateAppRequested()
    {
        CancelUpdateApp();
    }

    public void OnQuitRequested()
    {
        Quit();
    }
}