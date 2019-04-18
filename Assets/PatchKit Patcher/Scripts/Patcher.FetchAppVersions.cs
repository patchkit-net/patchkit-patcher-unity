using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task FetchAppVersions()
    {
        Assert.IsNotNull(value: State.AppState);

        // ReSharper disable once PossibleNullReferenceException
        var versions = await LibPatchKitApps.GetAppVersionListAsync(
            secret: State.AppState.Secret,
            cancellationToken: CancellationToken.None);

        ModifyState(x: () => State.AppState.Versions = versions);
    }
}