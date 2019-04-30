using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task FetchAppInstalledVersionId()
    {
        Debug.Log(message: "Fetching app installed version id...");

        Assert.IsNotNull(value: State.AppState);

        // ReSharper disable once PossibleNullReferenceException
        int? installedVersionId =
            await LibPatchKitApps.GetAppInstalledVersionIdAsync(
                path: State.AppState.Path,
                cancellationToken: CancellationToken.None);

        if (installedVersionId <= 0)
        {
            installedVersionId = null;
        }

        ModifyState(
            x: () => State.AppState.InstalledVersionId = installedVersionId);

        Debug.Log(
            message:
            $"Successfully fetched app installed version id: {installedVersionId?.ToString() ?? "null"}.");
    }
}