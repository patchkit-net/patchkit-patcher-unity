using System;
using System.Threading;
using System.Threading.Tasks;

public partial class Patcher
{
    private CancellationTokenSource _updateAppCancellationTokenSource;

    public void CancelUpdateApp()
    {
        if (State.Kind == PatcherStateKind.AskingForLicenseKey)
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
        if (State.Kind != PatcherStateKind.Idle &&
            State.Kind != PatcherStateKind.AskingForLicenseKey &&
            State.Kind != PatcherStateKind.Initializing)
        {
            return;
        }

        ModifyState(
            x: () =>
            {
                State.Kind = PatcherStateKind.UpdatingApp;
                State.UpdateAppState.TotalBytes = 0;
                State.UpdateAppState.InstalledBytes = 0;
                State.UpdateAppState.BytesPerSecond = 0;
                State.UpdateAppState.IsConnecting = true;
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
                            State.UpdateAppState.IsConnecting = false;
                            State.UpdateAppState.InstalledBytes =
                                progress.InstalledBytes;
                            State.UpdateAppState.TotalBytes =
                                progress.TotalBytes;
                            State.UpdateAppState.Progress =
                                progress.InstalledBytes /
                                (double) progress.TotalBytes;
                            State.UpdateAppState.BytesPerSecond =
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
                    State.Kind = PatcherStateKind.AskingForLicenseKey;
                    State.AppState.LicenseKeyIssue =
                        PatcherLicenseKeyIssue.None;
                });

            return;
        }
        catch (LibPatchKitAppsBlockedAppLicenseKeyException)
        {
            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.AskingForLicenseKey;
                    State.AppState.LicenseKeyIssue =
                        PatcherLicenseKeyIssue.Blocked;
                });

            return;
        }
        catch (LibPatchKitAppsInvalidAppLicenseKeyException)
        {
            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.AskingForLicenseKey;
                    State.AppState.LicenseKeyIssue =
                        PatcherLicenseKeyIssue.Invalid;
                });

            return;
        }
        catch (LibPatchKitAppsOutOfFreeDiskSpaceException)
        {
            ModifyState(
                x: () =>
                    State.Kind = PatcherStateKind.DisplyingOutOfDiskSpaceError);
            return;
        }

        await FetchAppInstalledVersionId();

        ModifyState(x: () => State.Kind = PatcherStateKind.Idle);
    }
}