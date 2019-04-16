using System.Threading;
using System.Threading.Tasks;

public partial class Patcher
{
    private async Task FetchAppVersions()
    {
        // ReSharper disable once PossibleNullReferenceException
        var versions = await LibPatchKitApps.GetAppVersionListAsync(
            secret: State.AppState.Secret,
            cancellationToken: CancellationToken.None);

        ModifyState(x: () => State.AppState.Versions = versions);
    }
}