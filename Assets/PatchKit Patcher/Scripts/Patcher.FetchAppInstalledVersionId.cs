using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task<bool> FetchAppInstalledVersionIdAsync()
    {
        if (!CanPerformNewTask() ||
            !_hasApp ||
            _hasAppFetchInstalledVersionIdTask)
        {
            return false;
        }

        Debug.Log(message: "Fetching app installed version id...");

        _hasAppFetchInstalledVersionIdTask = true;
        SendStateChanged();

        try
        {
            Assert.IsNotNull(value: _appPath);

            int? installedVersionId =
                await LibPatchKitApps.GetAppInstalledVersionIdAsync(
                    path: _appPath,
                    cancellationToken: CancellationToken.None);

            if (installedVersionId <= 0)
            {
                installedVersionId = null;
            }

            _appInstalledVersionId = installedVersionId;
            SendStateChanged();

            Debug.Log(
                message: "Successfully fetched app installed version id.");
        }
        catch (OperationCanceledException)
        {
            Debug.Log(
                message:
                "Failed to fetch app installed version id: operation cancelled.");

            return false;
        }
        catch (LibPatchKitAppsInternalErrorException)
        {
            Debug.LogWarning(
                message:
                "Failed to fetch app installed version id: internal error.");

            return false;
        }
        catch (LibPatchKitAppsUnauthorizedAccessException)
        {
            Debug.Log(
                message:
                "Failed to fetch app installed version id: unauthroized access.");

            return false;
        }
        catch (Exception e)
        {
            Debug.LogError(
                message:
                "Failed to fetch app installed version id: unknown error.");
            Debug.LogException(exception: e);

            return false;
        }
        finally
        {
            _hasAppFetchInstalledVersionIdTask = false;
            SendStateChanged();
        }

        return true;
    }
}