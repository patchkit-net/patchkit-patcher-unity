using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task<bool> FetchAppLatestVersionIdAsync()
    {
        if (!CanAppPerformNewTask() ||
            _hasAppFetchLatestVersionIdTask)
        {
            return false;
        }

        Debug.Log(message: "Fetching app latest version id...");

        _hasAppFetchLatestVersionIdTask = true;
        SendStateChanged();

        try
        {
            Assert.IsNotNull(_appSecret);

            int latestVersionId =
                await LibPatchKitApps.GetAppLatestVersionIdAsync(
                    secret: _appSecret,
                    cancellationToken: CancellationToken.None);

            _appLatestVersionId = latestVersionId;
            SendStateChanged();

            Debug.Log(message: "Successfully fetched app latest version id.");
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
        catch (Exception e)
        {
            Debug.LogError(
                message:
                "Failed to fetch app latest version id: unknown error.");
            Debug.LogException(exception: e);

            return false;
        }
        finally
        {
            _hasAppFetchLatestVersionIdTask = false;
            SendStateChanged();
        }

        return true;
    }
}