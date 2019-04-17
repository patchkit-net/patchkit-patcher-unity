using System.Threading;
using System.Threading.Tasks;

public partial class Patcher
{
    private async Task StartApp()
    {
        if (State.Kind != PatcherStateKind.Idle &&
            State.Kind != PatcherStateKind.Initializing)
        {
            return;
        }

        if (!State.AppState.InstalledVersionId.HasValue)
        {
            return;
        }

        ModifyState(x: () => State.Kind = PatcherStateKind.StartingApp);

        // ReSharper disable once PossibleNullReferenceException
        await LibPatchKitApps.StartAppAsync(
            path: State.AppState.Path,
            cancellationToken: CancellationToken.None);

        Quit();
    }
}