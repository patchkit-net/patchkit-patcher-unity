using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    [NotNull]
    private Task CancelUpdateApp2()
    {
        Debug.Log(message: "Cancelling updating app...");

        Assert.IsTrue(condition: State.Kind == PatcherStateKind.UpdatingApp);
        Assert.IsNotNull(value: State.AppState);
        Assert.IsNotNull(
            value: State.AppState.UpdateState.CancellationTokenSource);

        State.AppState.UpdateState.CancellationTokenSource.Cancel();

        Debug.Log(message: "Successfully cancelled updating app.");

        return Task.CompletedTask;
    }

    private async Task UpdateApp()
    {
        Debug.Log(message: "Updating app...");

        Assert.IsNotNull(value: State.AppState);
        Assert.IsTrue(condition: State.Kind == PatcherStateKind.Idle);

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

            State.AppState.UpdateState.CancellationTokenSource =
                new CancellationTokenSource();

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
                    cancellationToken: State.AppState.UpdateState
                        .CancellationTokenSource.Token);
            }
            else
            {
                // ReSharper disable once PossibleNullReferenceException
                await LibPatchKitApps.UpdateAppLatestAsync(
                    path: State.AppState.Path,
                    secret: State.AppState.Secret,
                    licenseKey: State.AppState.LicenseKey,
                    reportProgress: reportProgress,
                    cancellationToken: State.AppState.UpdateState
                        .CancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log(message: "Failed to update app: cancelled.");

            ModifyState(x: () => State.Kind = PatcherStateKind.Idle);

            return;
        }
        catch (LibPatchKitAppsAppLicenseKeyRequiredException)
        {
            Debug.Log(
                message: "Failed to update app: app license key required.");

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
            Debug.Log(
                message: "Failed to update app: blocked app license key.");

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
            Debug.Log(
                message: "Failed to update app: invalid app license key.");

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
            Debug.Log(message: "Failed to update app: out of disk space.");

            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.DisplayingError;
                    State.Error = PatcherError.OutOfDiskSpaceError;
                });
            return;
        }

        Debug.Log(message: "Successfully updated app.");

        await FetchAppInstalledVersionId();

        ModifyState(x: () => State.Kind = PatcherStateKind.Idle);
    }
}