using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task<bool> FetchAppLatestVersionIdAsync()
    {
        if (!CanPerformNewTask() ||
            !_hasApp ||
            _hasAppFetchLatestVersionTask)
        {
            return false;
        }

        Debug.Log(message: "Fetching app latest version id...");

        _hasAppFetchLatestVersionTask = true;
        SendStateChanged();

        try
        {
            int latestVersionId =
                await LibPatchKitApps.GetAppLatestVersionIdAsync(
                    path: _appPath,
                    cancellationToken: CancellationToken.None);

            _appLatestVersionId = latestVersionId;
            SendStateChanged();

            Debug.Log(
                message:
                $"Successfully fetched app latest version id.");
        }
        catch (OperationCanceledException)
        {
            Debug.Log(
                message: 
                "Failed to fetch app latest version id: operation cancelled.");

            return false;
        }
        catch (LibPatchKitAppsInternalErrorException)
        {
            Debug.LogWarning(
                message: 
                "Failed to fetch app latest version id: internal error.");

            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                message: 
                "Failed to fetch app latest version id: unknown error.");
            Debug.LogException(exception: e);

            return false;
        }
        finally
        {
            _hasAppFetchLatestVersionTask = false;
            SendStateChanged();
        }

        return true;
    }
}