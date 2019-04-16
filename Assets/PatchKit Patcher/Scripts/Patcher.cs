using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities;

public class Patcher : MonoBehaviour
{
    private static Patcher _instance;

    [NotNull]
    public static Patcher Instance
    {
        get
        {
            Assert.IsNotNull(value: _instance);
            return _instance;
        }
    }

    public PatcherConfiguration Configuration;

#if UNITY_EDITOR
    public string EditorAppSecret;
    public int EditorOverrideAppLatestVersionId;
#endif

    private PatcherState _state;

    [NotNull]
    public PatcherState State
    {
        get
        {
            Assert.IsNotNull(value: _state);
            return _state;
        }
    }

    public delegate void OnPatcherStateChanged([NotNull] PatcherState state);

    public event OnPatcherStateChanged StateChanged;

    private void InitializeLibPatchKitApps()
    {
        bool is64Bit = IntPtr.Size == 8;

        if (Application.platform == RuntimePlatform.LinuxEditor ||
            Application.platform == RuntimePlatform.LinuxPlayer)
        {
            LibPatchKitApps.SetPlatformType(
                platformType: is64Bit
                    ? LibPatchKitAppsPlatformType.Linux64
                    : LibPatchKitAppsPlatformType.Linux32);
        }

        if (Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer)
        {
            LibPatchKitApps.SetPlatformType(
                platformType: is64Bit
                    ? LibPatchKitAppsPlatformType.Win32
                    : LibPatchKitAppsPlatformType.Win64);
        }

        if (Application.platform == RuntimePlatform.OSXEditor ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            LibPatchKitApps.SetPlatformType(
                platformType: LibPatchKitAppsPlatformType.Osx64);
        }
    }

    private void InitializeState()
    {
#if UNITY_EDITOR
        Assert.IsNotNull(value: Application.dataPath);

        var data = new PatcherData
        {
            AppSecret = EditorAppSecret,
            AppDataPath = Application.dataPath.Replace(
                oldValue: "/Assets",
                newValue: $"/Temp/PatcherApp{EditorAppSecret}"),
            OverrideLatestVersionId = EditorOverrideAppLatestVersionId
        };
#else
        var data = new InputArgumentsPatcherDataReader().Read();
#endif
        _state = new PatcherState(
            appSecret: data.AppSecret,
            appPath: data.AppDataPath,
            lockFilePath: data.LockFilePath,
            overrideAppLatestVersionId: data.OverrideLatestVersionId > 0
                ? (int?) data.OverrideLatestVersionId
                : null)
        {
            Kind = PatcherStateKind.Initializing,
            IsOnline = data.IsOnline ?? true,
            AppState =
            {
                ShouldBeUpdatedAutomatically =
                    Configuration.AutomaticallyUpdateApp,
                ShouldBeStartedAutomatically =
                    Configuration.AutomaticallyStartApp
            },
            HasChanged = true
        };
    }

    private void Awake()
    {
        _instance = this;

        Assert.raiseExceptions = true;
        Application.runInBackground = true;
        UnityDispatcher.Initialize();

        InitializeLibPatchKitApps();
        InitializeState();
    }

    private async void Start()
    {
        await SafeInvoke(
            func: async () =>
            {
                var updates = Task.WhenAll(
                    UpdateAppInfo(),
                    UpdateAppVersions(),
                    UpdateAppLatestVersionId(),
                    UpdateAppInstalledVersionId());

                Assert.IsNotNull(value: updates);

                if (State.AppState.ShouldBeUpdatedAutomatically)
                {
                    await UpdateApp();
                }

                if (State.AppState.ShouldBeStartedAutomatically)
                {
                    await StartApp();
                }

                State.Kind = PatcherStateKind.Idle;
                State.HasChanged = true;

                await updates;
            });
    }

    private void Update()
    {
        if (State.HasChanged)
        {
            State.HasChanged = false;
            StateChanged?.Invoke(state: State);
        }
    }

    private async Task SafeInvoke([NotNull] Func<Task> func)
    {
        try
        {
            var t = func();
            Assert.IsNotNull(value: t);
            await t;
        }
        // ReSharper disable once RedundantCatchClause
        catch (LibPatchKitAppsInternalErrorException)
        {
            // TODO: Display error and quit the app
            throw;
        }
        // ReSharper disable once RedundantCatchClause
        catch (LibPatchKitAppsUnauthorizedAccessException)
        {
            // TODO: Display error and quit the app
            throw;
        }
    }

    private async Task StartApp()
    {
        if (State.Kind != PatcherStateKind.Initializing &&
            State.Kind != PatcherStateKind.Idle)
        {
            return;
        }

        if (!State.AppState.InstalledVersionId.HasValue)
        {
            return;
        }

        State.Kind = PatcherStateKind.StartingApp;
        State.HasChanged = true;

        // ReSharper disable once PossibleNullReferenceException
        await LibPatchKitApps.StartAppAsync(
            path: State.AppState.Path,
            cancellationToken: CancellationToken.None);

        Close();
    }

    private CancellationTokenSource _updateAppCancellationTokenSource;

    public void CancelUpdateApp()
    {
        _updateAppCancellationTokenSource?.Cancel();
    }

    private async Task UpdateApp()
    {
        if (State.Kind != PatcherStateKind.Initializing &&
            State.Kind != PatcherStateKind.Idle)
        {
            return;
        }

        State.Kind = PatcherStateKind.UpdatingApp;
        State.UpdateAppState.TotalBytes = 0;
        State.UpdateAppState.InstalledBytes = 0;
        State.UpdateAppState.BytesPerSecond = 0;
        State.UpdateAppState.IsConnecting = true;
        State.HasChanged = true;

        try
        {
            if (State.AppState.OverrideLatestVersionId.HasValue)
            {
                // ReSharper disable once PossibleNullReferenceException
                await LibPatchKitApps.UpdateAppAsync(
                    path: State.AppState.Path,
                    secret: State.AppState.Secret,
                    licenseKey: State.AppState.LicenseKey,
                    targetVersionId: State.AppState.OverrideLatestVersionId
                        .Value,
                    reportProgress: ReportUpdateAppProgress,
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
                    reportProgress: ReportUpdateAppProgress,
                    cancellationToken: (_updateAppCancellationTokenSource =
                        new CancellationTokenSource()).Token);
            }
        }
        catch (OperationCanceledException)
        {
            State.Kind = PatcherStateKind.Idle;
            State.HasChanged = true;

            return;
        }
        catch (LibPatchKitAppsAppLicenseKeyRequiredException)
        {
            State.Kind = PatcherStateKind.AskingForLicenseKey;
            State.AppState.LicenseKeyIssue = PatcherLicenseKeyIssue.None;
            State.HasChanged = true;

            return;
        }
        catch (LibPatchKitAppsBlockedAppLicenseKeyException)
        {
            State.Kind = PatcherStateKind.AskingForLicenseKey;
            State.AppState.LicenseKeyIssue = PatcherLicenseKeyIssue.Blocked;
            State.HasChanged = true;

            return;
        }
        catch (LibPatchKitAppsInvalidAppLicenseKeyException)
        {
            State.Kind = PatcherStateKind.AskingForLicenseKey;
            State.AppState.LicenseKeyIssue = PatcherLicenseKeyIssue.Invalid;
            State.HasChanged = true;

            return;
        }
        catch (LibPatchKitAppsOutOfFreeDiskSpaceException)
        {
            // TODO: Do something
        }

        await UpdateAppInstalledVersionId();

        State.Kind = PatcherStateKind.Idle;
        State.HasChanged = true;
    }

    private void ReportUpdateAppProgress(
        LibPatchKitAppsUpdateAppProgress progress)
    {
        State.UpdateAppState.IsConnecting = false;
        State.UpdateAppState.InstalledBytes = progress.InstalledBytes;
        State.UpdateAppState.TotalBytes = progress.TotalBytes;
        State.UpdateAppState.Progress =
            progress.InstalledBytes / (double) progress.TotalBytes;
        State.HasChanged = true;
    }

    private void Close()
    {
        if (State.Kind != PatcherStateKind.Idle &&
            State.Kind != PatcherStateKind.StartingApp)
        {
            return;
        }

        State.Kind = PatcherStateKind.Quitting;

#if UNITY_EDITOR
        if (Application.isEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
        else
#endif
        {
            Application.Quit();
        }
    }

    private async Task UpdateAppInstalledVersionId()
    {
        // ReSharper disable once PossibleNullReferenceException
        int x = await LibPatchKitApps.GetAppInstalledVersionIdAsync(
            path: State.AppState.Path,
            cancellationToken: CancellationToken.None);

        State.AppState.InstalledVersionId = x > 0 ? (int?) x : null;
        State.HasChanged = true;
    }

    private async Task UpdateAppVersions()
    {
        State.AppState.Versions =
            // ReSharper disable once PossibleNullReferenceException
            await LibPatchKitApps.GetAppVersionListAsync(
                secret: State.AppState.Secret,
                cancellationToken: CancellationToken.None);

        State.HasChanged = true;
    }

    private async Task UpdateAppInfo()
    {
        State.AppState.Info =
            // ReSharper disable once PossibleNullReferenceException
            await LibPatchKitApps.GetAppInfoAsync(
                secret: State.AppState.Secret,
                cancellationToken: CancellationToken.None);

        State.HasChanged = true;
    }

    private async Task UpdateAppLatestVersionId()
    {
        if (State.AppState.OverrideLatestVersionId.HasValue)
        {
            State.AppState.LatestVersionId =
                State.AppState.OverrideLatestVersionId.Value;
            return;
        }

        State.AppState.LatestVersionId =
            // ReSharper disable once PossibleNullReferenceException
            await LibPatchKitApps.GetAppLatestVersionIdAsync(
                secret: State.AppState.Secret,
                cancellationToken: CancellationToken.None);
        State.HasChanged = true;
    }

    public async void OnUpdateAppRequested()
    {
        await SafeInvoke(func: UpdateApp);
    }

    public void OnCancelUpdateAppRequested()
    {
        CancelUpdateApp();
    }

    public async void OnStartAppRequested()
    {
        await SafeInvoke(func: StartApp);
    }

    public void OnCloseRequested()
    {
        Close();
    }

    //TODO: Move into Legacy
    public void Quit()
    {
        OnCloseRequested();
    }
}