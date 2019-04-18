using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task FetchAppLatestVersionId()
    {
        Assert.IsNotNull(value: State.AppState);

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