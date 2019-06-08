using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task<bool> FetchAppVersionsAsync()
    {
        if (!CanPerformNewTask() ||
            !_hasApp ||
            _hasAppFetchAppVersionsTask)
        {
            return false;
        }

        Debug.Log(message: "Fetching app versions...");

        _hasAppFetchAppVersionsTask = true;
        SendStateChanged();

        try
        {
            var versions = await LibPatchKitApps.GetAppVersionsAsync(
                secret: State.AppState.Secret,
                cancellationToken: CancellationToken.None);

            _appVersions = versions;
            SendStateChanged();

            Debug.Log(message: "Successfully fetched app versions.");
        }
        catch (OperationCanceledException)
        {
            Debug.Log(
                message: "Failed to fetch app versions: operation cancelled.");

            return false;
        }
        catch (LibPatchKitAppsInternalErrorException)
        {
            Debug.LogWarning(
                message: "Failed to fetch app versions: internal error.");

            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError(message: "Failed to fetch app versions: unknown error.");
            Debug.LogException(exception: e);

            return false;
        }
        finally
        {
            _hasAppFetchAppVersionsTask = false;
            SendStateChanged();
        }

        return true;
    }
}