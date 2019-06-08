using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task<bool> StartAppAsync()
    {
        if (!CanPerformNewForegroundTask() ||
            !_hasApp)
        {
            return false;
        }

        Debug.Log(message: "Starting app...");
        _hasAppStartTask = true;
        SendStateChanged();

        try
        {
            await LibPatchKitApps.StartAppAsync(
                path: _appPath,
                cancellationToken: CancellationToken.None);

            Debug.Log(message: "Successfully started app.");
        }
        catch (OperationCanceledException)
        {
            Debug.Log(message: "Failed to start app: operation cancelled.")

            return false;
        }
        catch (LibPatchKitAppsInternalErrorException)
        {
            Debug.LogWarning(message: "Failed to start app: internal error.");

            SendError(error: Error.StartAppError);

            return false;
        }
        catch (LibPatchKitAppsUnauthorizedAccessException)
        {
            Debug.Log(message: "Failed to start app: unauthorized access.")

            SendError(error: Error.AppDataUnauthorizedAccess);

            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError(message: "Failed to start app: unknown error.")
            Debug.LogException(exception: e);

            SendError(error: Error.StartAppError);

            return false;
        }
        finally
        {
            _hasAppStartTask = false;
            SendStateChanged();
        }

        return true;
    }
}