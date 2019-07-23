using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public partial class Patcher
{
    private async Task<bool> UpdateAppAsync()
    {
        if (!CanAppPerformNewForegroundTask())
        {
            return false;
        }

        Debug.Log(message: "Updating app...");
        _hasAppUpdateTask = true;
        _appUpdateTaskTotalBytes = 0;
        _appUpdateTaskInstalledBytes = 0;
        _appUpdateTaskBytesPerSecond = 0.0;
        SendStateChanged();

        try
        {
            DateTime? lastReportProgress = null;
            long lastInstalledBytes = 0;

            var reportProgressMinSpan = TimeSpan.FromSeconds(value: 1);

            Action<LibPatchKitAppsUpdateAppProgress> reportProgress =
                progress =>
                {
                    double bytesPerSecond = 0.0;

                    var now = DateTime.Now;

                    if (lastReportProgress.HasValue)
                    {
                        var span = now - lastReportProgress.Value;

                        if (span < reportProgressMinSpan)
                        {
                            return;
                        }

                        bytesPerSecond =
                            (progress.InstalledBytes - lastInstalledBytes) /
                            span.TotalSeconds;
                    }

                    lastReportProgress = now;
                    lastInstalledBytes = progress.InstalledBytes;

                    _appUpdateTaskTotalBytes = progress.TotalBytes;
                    _appUpdateTaskInstalledBytes = progress.InstalledBytes;
                    _appUpdateTaskBytesPerSecond = bytesPerSecond;
                    SendStateChanged();
                };

            _appUpdateTaskCts = new CancellationTokenSource();

            if (_appOverrideLatestVersionId.HasValue)
            {
                await LibPatchKitApps.UpdateAppAsync(
                    path: _appPath,
                    secret: _appSecret,
                    licenseKey: _appLicenseKey,
                    targetVersionId: _appOverrideLatestVersionId.Value,
                    reportProgress: reportProgress,
                    cancellationToken: _appUpdateTaskCts.Token);
            }
            else
            {
                await LibPatchKitApps.UpdateAppLatestAsync(
                    path: _appPath,
                    secret: _appSecret,
                    licenseKey: _appLicenseKey,
                    reportProgress: reportProgress,
                    cancellationToken: _appUpdateTaskCts.Token);
            }

            Debug.Log(message: "Successfully updated app.");
        }
        catch (OperationCanceledException)
        {
            Debug.Log(message: "Failed to update app: cancelled.");

            return false;
        }
        catch (LibPatchKitAppsInternalErrorException)
        {
            Debug.LogWarning(message: "Failed to update app: internal error.");

            SendError(error: Error.UpdateAppError);

            return false;
        }
        catch (LibPatchKitAppsUnauthorizedAccessException)
        {
            Debug.Log(message: "Failed to update app: unauthorized access.");

            SendError(error: Error.AppDataUnauthorizedAccess);

            return false;
        }
        catch (LibPatchKitAppsOutOfFreeDiskSpaceException)
        {
            Debug.Log(message: "Failed to update app: out of free disk space.");

            SendError(error: Error.UpdateAppRunOutOfFreeDiskSpace);

            return false;
        }
        catch (LibPatchKitAppsAppLicenseKeyRequiredException)
        {
            Debug.Log(
                message: "Failed to update app: app license key required.");

            SendAppLicenseKeyIssue(issue: AppLicenseKeyIssue.None);

            return false;
        }
        catch (LibPatchKitAppsBlockedAppLicenseKeyException)
        {
            Debug.Log(
                message: "Failed to update app: blocked app license key.");

            SendAppLicenseKeyIssue(issue: AppLicenseKeyIssue.Blocked);

            return false;
        }
        catch (LibPatchKitAppsInvalidAppLicenseKeyException)
        {
            Debug.Log(
                message: "Failed to update app: invalid app license key.");

            SendAppLicenseKeyIssue(issue: AppLicenseKeyIssue.Invalid);

            return false;
        }
        catch (Exception e)
        {
            Debug.LogError(message: "Failed to update app: unknown error.");
            Debug.LogException(e);

            SendError(error: Error.UpdateAppError);

            return false;
        }
        finally
        {
            _hasAppUpdateTask = false;
            SendStateChanged();
        }

        await FetchAppInstalledVersionIdAsync();
        await FetchAppLatestVersionIdAsync();
        await FetchAppVersionsAsync();
        await FetchAppInfoAsync();

        return true;
    }
}