using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task FetchAppInfo()
    {
        Debug.Log(message: "Fetching app info...");

        Assert.IsNotNull(value: State.AppState);

        // ReSharper disable once PossibleNullReferenceException
        var info = await LibPatchKitApps.GetAppInfoAsync(
            secret: State.AppState.Secret,
            cancellationToken: CancellationToken.None);

        ModifyState(x: () => State.AppState.Info = info);

        Debug.Log(message: "Successfully fetched app info.");
    }
}