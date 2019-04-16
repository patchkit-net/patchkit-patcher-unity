using System.Threading;
using System.Threading.Tasks;

public partial class Patcher
{
    private async Task FetchAppLatestVersionId()
    {
        int latestVersionId;

        if (State.AppState.OverrideLatestVersionId.HasValue)
        {
            latestVersionId = State.AppState.OverrideLatestVersionId.Value;
        }
        else
        {
            // ReSharper disable once PossibleNullReferenceException
            latestVersionId = await LibPatchKitApps.GetAppLatestVersionIdAsync(
                secret: State.AppState.Secret,
                cancellationToken: CancellationToken.None);
        }

        ModifyState(x: () => State.AppState.LatestVersionId = latestVersionId);
    }
}