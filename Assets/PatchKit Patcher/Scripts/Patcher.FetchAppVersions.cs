using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task<bool> FetchAppVersionsAsync()
    {
        if (!CanAppPerformNewTask() ||
            _hasAppFetchVersionsTask)
        {
            return false;
        }

        Debug.Log(message: "Fetching app versions...");

        _hasAppFetchVersionsTask = true;
        SendStateChanged();

        try
        {
            Assert.IsNotNull(value: _appSecret);

            var versions = await LibPatchKitApps.GetAppVersionListAsync(
                secret: _appSecret,
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
        catch (Exception e)
        {
            Debug.LogError(
                message: "Failed to fetch app versions: unknown error.");
            Debug.LogException(exception: e);

            return false;
        }
        finally
        {
            _hasAppFetchVersionsTask = false;
            SendStateChanged();
        }

        return true;
    }
}