using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;

public partial class Patcher
{
    private LibPatchKitAppsFileLock _fileLock;

    private async Task Startup()
    {
        try
        {
            // ReSharper disable once PossibleNullReferenceException
            _fileLock = await LibPatchKitApps.GetFileLockAsync(
                path: State.LockFilePath,
                cancellationToken: CancellationToken.None);
        }
        catch (LibPatchKitAppsFileAlreadyInUseException)
        {
            ModifyState(
                x: () =>
                    State.Kind = PatcherStateKind
                        .DisplayingMultipleInstancesError);
            return;
        }
        catch (LibPatchKitAppsNotExistingFileException)
        {
        }

        var updates = Task.WhenAll(
            FetchAppInfo(),
            FetchAppVersions(),
            FetchAppLatestVersionId(),
            FetchAppInstalledVersionId());

        Assert.IsNotNull(value: updates);

        if (State.AppState.ShouldBeUpdatedAutomatically)
        {
            await UpdateApp();
        }

        if (State.AppState.ShouldBeStartedAutomatically)
        {
            await StartApp();
        }

        ModifyState(x: () => State.Kind = PatcherStateKind.Idle);

        await updates;
    }
}