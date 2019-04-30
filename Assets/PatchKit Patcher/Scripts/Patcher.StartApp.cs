using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task StartApp()
    {
        Debug.Log(message: "Starting app...");

        Assert.IsNotNull(value: State.AppState);
        Assert.IsTrue(condition: State.Kind == PatcherStateKind.Idle);

        if (!State.AppState.InstalledVersionId.HasValue)
        {
            Debug.Log(
                message: "App couldn't be started because it's not installed.");

            return;
        }

        ModifyState(x: () => State.Kind = PatcherStateKind.StartingApp);

        // ReSharper disable once PossibleNullReferenceException
        await LibPatchKitApps.StartAppAsync(
            path: State.AppState.Path,
            cancellationToken: CancellationToken.None);

        Debug.Log(message: "Successfully started app.");

        await Quit2();
    }
}