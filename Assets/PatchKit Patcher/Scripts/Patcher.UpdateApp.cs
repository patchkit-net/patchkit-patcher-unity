using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Assertions;

public partial class Patcher
{
    private CancellationTokenSource _updateAppCancellationTokenSource;

    public void CancelUpdateApp()
    {
        if (State.Kind == PatcherStateKind.AskingForAppLicenseKey)
        {
            ModifyState(x: () => State.Kind = PatcherStateKind.Idle);
        }
        else
        {
            _updateAppCancellationTokenSource?.Cancel();
        }
    }

    private async Task UpdateApp()
    {
        Assert.IsNotNull(value: State.AppState);

        ModifyState(
            x: () =>
            {
                State.Kind = PatcherStateKind.UpdatingApp;
                State.AppState.UpdateState.TotalBytes = 0;
                State.AppState.UpdateState.InstalledBytes = 0;
                State.AppState.UpdateState.BytesPerSecond = 0;
                State.AppState.UpdateState.IsConnecting = true;
            });

        try
        {
            DateTime? lastReportProgress = null;
            long lastInstalledBytes = 0;

            var reportProgressDelay = TimeSpan.FromSeconds(value: 1);

            Action<LibPatchKitAppsUpdateAppProgress> reportProgress =
                progress =>
                {
                    double bytesPerSecond = 0.0;

                    var now = DateTime.Now;

                    if (lastReportProgress.HasValue)
                    {
                        var span = now - lastReportProgress.Value;

                        if (span < reportProgressDelay)
                        {
                            return;
                        }

                        bytesPerSecond =
                            (progress.InstalledBytes - lastInstalledBytes) /
                            span.TotalSeconds;
                    }

                    lastReportProgress = now;
                    lastInstalledBytes = progress.InstalledBytes;

                    ModifyState(
                        x: () =>
                        {
                            State.AppState.UpdateState.IsConnecting = false;
                            State.AppState.UpdateState.InstalledBytes =
                                progress.InstalledBytes;
                            State.AppState.UpdateState.TotalBytes =
                                progress.TotalBytes;
                            State.AppState.UpdateState.Progress =
                                progress.InstalledBytes /
                                (double) progress.TotalBytes;
                            State.AppState.UpdateState.BytesPerSecond =
                                bytesPerSecond;
                        });
                };

            if (State.AppState.OverrideLatestVersionId.HasValue)
            {
                // ReSharper disable once PossibleNullReferenceException
                await LibPatchKitApps.UpdateAppAsync(
                    path: State.AppState.Path,
                    secret: State.AppState.Secret,
                    licenseKey: State.AppState.LicenseKey,
                    targetVersionId: State.AppState.OverrideLatestVersionId
                        .Value,
                    reportProgress: reportProgress,
                    cancellationToken: (_updateAppCancellationTokenSource =
                        new CancellationTokenSource()).Token);
            }
            else
            {
                // ReSharper disable once PossibleNullReferenceException
                await LibPatchKitApps.UpdateAppLatestAsync(
                    path: State.AppState.Path,
                    secret: State.AppState.Secret,
                    licenseKey: State.AppState.LicenseKey,
                    reportProgress: reportProgress,
                    cancellationToken: (_updateAppCancellationTokenSource =
                        new CancellationTokenSource()).Token);
            }
        }
        catch (OperationCanceledException)
        {
            ModifyState(x: () => State.Kind = PatcherStateKind.Idle);

            return;
        }
        catch (LibPatchKitAppsAppLicenseKeyRequiredException)
        {
            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.AskingForAppLicenseKey;
                    State.AppState.LicenseKeyIssue =
                        PatcherAppLicenseKeyIssue.None;
                });

            return;
        }
        catch (LibPatchKitAppsBlockedAppLicenseKeyException)
        {
            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.AskingForAppLicenseKey;
                    State.AppState.LicenseKeyIssue =
                        PatcherAppLicenseKeyIssue.Blocked;
                });

            return;
        }
        catch (LibPatchKitAppsInvalidAppLicenseKeyException)
        {
            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.AskingForAppLicenseKey;
                    State.AppState.LicenseKeyIssue =
                        PatcherAppLicenseKeyIssue.Invalid;
                });

            return;
        }
        catch (LibPatchKitAppsOutOfFreeDiskSpaceException)
        {
            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.DisplayingError;
                    State.Error = PatcherError.OutOfDiskSpaceError;
                });
            return;
        }

        await FetchAppInstalledVersionId();

        ModifyState(x: () => State.Kind = PatcherStateKind.Idle);
    }
}