using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task FetchAppLatestVersionId()
    {
        Debug.Log(message: "Fetching app latest version id...");

        Assert.IsNotNull(value: State.AppState);

        int latestVersionId;

        if (State.AppState.OverrideLatestVersionId.HasValue)
        {
            Debug.Log(
                message:
                "Using override value for fetching app latest version id.");

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

        Debug.Log(
            message:
            $"Successfully fetched app latest version id: {latestVersionId}.");
    }
}