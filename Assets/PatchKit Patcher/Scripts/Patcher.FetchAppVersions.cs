using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task FetchAppVersions()
    {
        Debug.Log(message: "Fetching app latest version id...");

        Assert.IsNotNull(value: State.AppState);

        // ReSharper disable once PossibleNullReferenceException
        var versions = await LibPatchKitApps.GetAppVersionListAsync(
            secret: State.AppState.Secret,
            cancellationToken: CancellationToken.None);

        ModifyState(x: () => State.AppState.Versions = versions);

        Debug.Log(message: "Successfully fetched app versions.");
    }
}