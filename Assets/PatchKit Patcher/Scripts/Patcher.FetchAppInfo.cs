using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task<bool> FetchAppInfoAsync()
    {
        if (!CanPerformNewTask() ||
            !_hasApp ||
            _hasAppFetchInfoTask)
        {
            return false;
        }

        Debug.Log(message: "Fetching app info...");

        _hasAppFetchInfoTask = true;
        SendStateChanged();

        try
        {
            Assert.IsNotNull(value: _appSecret);

            var info = await LibPatchKitApps.GetAppInfoAsync(
                secret: _appSecret,
                cancellationToken: CancellationToken.None);

            _appInfo = info;
            SendStateChanged();

            Debug.Log(message: "Successfully fetched app info.");
        }
        catch (OperationCanceledException)
        {
            Debug.Log(
                message: "Failed to fetch app info: operation cancelled.");

            return false;
        }
        catch (LibPatchKitAppsInternalErrorException)
        {
            Debug.LogWarning(
                message: "Failed to fetch app info: internal error.");

            return false;
        }
        catch (Exception e)
        {
            Debug.LogError(message: "Failed to fetch app info: unknown error.");
            Debug.LogException(exception: e);

            return false;
        }
        finally
        {
            _hasAppFetchInfoTask = false;
            SendStateChanged();
        }

        return true;
    }
}