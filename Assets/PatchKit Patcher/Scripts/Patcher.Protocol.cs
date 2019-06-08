using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public enum AppLicenseKeyIssue
{
    None,
    Invalid,
    Blocked
}

public enum Error
{
    CriticalError,
    StartedWithoutLauncher,
    MultipleInstances,
    AppDataUnauthorizedAccess,
    UpdateAppRunOutOfDiskSpace,
    UpdateAppError,
    StartAppError
}

public delegate void OnStateChangedDelegate(State state);

public delegate void OnAppLicenseKeyIssueDelegate(
    string licenseKey, 
    AppLicenseKeyIssue issue);

public delegate void OnErrorDelegate(Error error);

// TODO: Manually invoke events through invocation list
// and surround with try in order to increase safety
public partial class Patcher
{
    public event OnStateChangedDelegate OnStateChanged;

    public event OnErrorDelegate OnError;

    public event OnAppLicenseKeyIssueDelegate OnAppLicenseKeyIssue;

    public async void RequestUpdateApp()
    {
        Debug.Log($"Request: UpdateApp()")

        await UpdateAppAsync();
    }

    public void RequestCancelUpdateApp()
    {
        Debug.Log($"Request: CancelUpdateApp()")

        CancelUpdateApp();
    }

    public void RequestStartAppAndQuit()
    {
        Debug.Log($"Request: StartAppAndQuit()")

        await StartAppAndQuitAsync();
    }

    public void RequestSetAppLicenseKeyAndUpdateApp(string licenseKey)
    {
        Debug.Log($"Request: SetAppLicenseKeyAndUpdateApp(licenseKey: {licenseKey})")

        await SetAppLicenseKeyAndUpdateAppAsync(licenseKey: licenseKey);
    }

    public async void RequestQuit()
    {
        Debug.Log("Request: Quit()")

        await QuitAsync();
    }

    public async void RequestRestartWithHigherPermissions()
    {
        Debug.Log("Request: RestartWithHigherPermissions()")

        await RestatWithHigherPermissions();
    }

    private double AppUpdateTaskProgress
    {
        get
        {
            return _appUpdateTaskInstalledBytes / 
                (double) _appUpdateTaskTotalBytes;
        }
    }

    private void SendStateChanged()
    {
        var state = new State(
            app: !_hasApp ? null : new AppState(
                secret: _appSecret,
                name: _appName,
                path: _appPath,
                info: _appInfo,
                versions: _appVersions,
                installedVersionId: _appInstalledVersionId,
                installedVersionLabel: null,
                latestVersionId: _appLatestVersionId,
                latestVersionLabel: null,
                updateTask: !_hasAppUpdateTask ? null : new AppUpdateTaskState(
                    totalBytes: _appUpdateTaskTotalBytes,
                    installedBytes: _appUpdateTaskInstalledBytes,
                    bytesPerSecond: _appUpdateTaskBytesPerSecond,
                    progress: AppUpdateTaskProgress,
                    isConnecting: false)),
            isOnline: _isOnline);

        Debug.Log($"Sending: OnStateChanged(state: {state})");

        OnStateChanged?.Invoke(state: state);
    }


    private void SendError(Error error)
    {
        Debug.Log($"Sending: OnError(error: {error})");

        OnError?.Invoke(error: error);
    }

    private void SendAppLicenseKeyIssue(AppLicenseKeyIssue issue)
    {
        Debug.Log($"Sending: OnAppLicenseKeyIssue(issue: {issue})");

        OnAppLicenseKeyIssue?.Invoke(
            licenseKey:_appLicenseKey, 
            issue: issue);
    }
}