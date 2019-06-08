using UnityEngine;

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
    UpdateAppRunOutOfFreeDiskSpace,
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
        Debug.Log(message: "Request: UpdateApp()");

        await UpdateAppAsync();
    }

    public async void RequestCancelUpdateApp()
    {
        Debug.Log(message: "Request: CancelUpdateApp()");

        await CancelUpdateAppAsync();
    }

    public async void RequestStartAppAndQuit()
    {
        Debug.Log(message: "Request: StartAppAndQuit()");

        await StartAppAndQuitAsync();
    }

    public async void RequestSetAppLicenseKeyAndUpdateApp(string licenseKey)
    {
        Debug.Log(
            message:
            $"Request: SetAppLicenseKeyAndUpdateApp(licenseKey: {licenseKey})");

        await SetAppLicenseKeyAndUpdateAppAsync(licenseKey: licenseKey);
    }

    public async void RequestQuit()
    {
        Debug.Log(message: "Request: Quit()");

        await QuitAsync();
    }

    public async void RequestRestartWithHigherPermissions()
    {
        Debug.Log(message: "Request: RestartWithHigherPermissions()");

        await RestartWithHigherPermissionsAsync();
    }

    public async void RequestRestartWithLauncher()
    {
        Debug.Log(message: "Request: RestartWithLauncher()");

        await RestartWithLauncherAsync();
    }

    private double AppUpdateTaskProgress =>
        _appUpdateTaskInstalledBytes / (double) _appUpdateTaskTotalBytes;

    private void SendStateChanged()
    {
        var state = new State(
            isInitializing: _hasInitializeTask,
            app: !_hasApp
                ? null
                : (AppState?) new AppState(
                    secret: _appSecret,
                    info: _appInfo,
                    versions: _appVersions,
                    installedVersionId: _appInstalledVersionId,
                    installedVersionLabel: null,
                    latestVersionId: _appLatestVersionId,
                    latestVersionLabel: null,
                    updateTask: !_hasAppUpdateTask
                        ? null
                        : (AppUpdateTaskState?) new AppUpdateTaskState(
                            totalBytes: _appUpdateTaskTotalBytes,
                            installedBytes: _appUpdateTaskInstalledBytes,
                            bytesPerSecond: _appUpdateTaskBytesPerSecond,
                            progress: AppUpdateTaskProgress,
                            isConnecting: false),
                    isStarting: _hasAppStartTask),
            isOnline: _isOnline,
            isQuitting: _hasQuitTask);

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
            licenseKey: _appLicenseKey,
            issue: issue);
    }
}