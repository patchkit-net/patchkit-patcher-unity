using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task StartApp()
    {
        Assert.IsNotNull(value: State.AppState);

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