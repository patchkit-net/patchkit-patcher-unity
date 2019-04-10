using System;
using System.Threading;
using JetBrains.Annotations;
using PatchKit.Unity.Patcher;
using PatchKit.Unity.Patcher.AppUpdater;
using UnityEngine;
using UnityEngine.Assertions;

namespace PatchKit_Patcher.Scripts
{
public class PatcherNew : MonoBehaviour
{
    public static PatcherNew Instance { get; private set; }

    public PatcherConfiguration Configuration;

    public string EditorAppSecret;
    public int EditorOverrideAppLatestVersionId;

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

    public event Action<PatcherState> StateChanged;

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
            var inputArgumentsPatcherDataReader =
 new InputArgumentsPatcherDataReader();
            var data = inputArgumentsPatcherDataReader.Read();
#endif

        _state = new PatcherState(
            appSecret: data.AppSecret,
            appPath: data.AppDataPath,
            lockFilePath: data.LockFilePath,
            overrideAppLatestVersionId: data.OverrideLatestVersionId > 0
                ? (int?) data.OverrideLatestVersionId
                : null)
        {
            Kind = PatcherStateKind.Idle,
            IsOnline = data.IsOnline ?? true
        };
    }

    private void Awake()
    {
        Instance = this;

        Assert.raiseExceptions = true;
        Application.runInBackground = true;

        InitializeLibPatchKitApps();
        InitializeState();
    }

    private void Start()
    {
        BeginGetAppLatestVersionId();
        BeginGetAppInfo();
        BeginGetAppVersionList();
        BeginGetAppInstalledVersionId();
    }

    private void Update()
    {
        UpdateGetAppLatestVersionId();
        UpdateGetAppInfo();
        UpdateGetAppVersionList();
        UpdateGetAppInstalledVersionId();

        if (State.HasChanged)
        {
            State.HasChanged = false;
            StateChanged?.Invoke(obj: State);
        }
    }

    private async void Shoot()
    {
        var task = LibPatchKitApps.StartAppAsync(
            path: "something",
            cancellationToken: CancellationToken.None);

        Assert.IsNotNull(task);

        await task;
    }

    private LibPatchKitAppsUpdateAppContext _updateAppCtx;

    private void BeginUpdateApp()
    {
        Assert.IsTrue(condition: State.Kind == PatcherStateKind.Idle);

        State.Kind = PatcherStateKind.UpdatingApp;
        State.UpdateAppState.InstalledBytes = 0;
        State.UpdateAppState.TotalBytes = 0;
        State.UpdateAppState.BytesPerSecond = 0;

        _hasStateChanged = true;

        _updateAppCtx?.Dispose();
        if (State.AppState.OverrideLatestVersionId.HasValue)
        {
            _updateAppCtx = LibPatchKitApps.UpdateApp(
                path: State.AppState.Path,
                secret: State.AppState.Secret,
                licenseKey: null,
                targetVersionId: State.AppState.OverrideLatestVersionId.Value);
        }
        else
        {
            _updateAppCtx = LibPatchKitApps.UpdateAppLatest(
                path: State.AppState.Path,
                secret: State.AppState.Secret,
                licenseKey: null);
        }
    }

    private void UpdateUpdateApp()
    {
        if (_updateAppCtx == null || _updateAppCtx.IsExecuting)
        {
            return;
        }

        if (_updateAppCtx.Error == LibPatchKitAppsUpdateAppError.None)
        {
            BeginGetAppInstalledVersionId();

            State.AppState.IsInstalled = true;
            State.Kind = PatcherStateKind.Idle;
            _hasStateChanged = true;
        }

        _updateAppCtx?.Dispose();
        _updateAppCtx = null;
    }

    private LibPatchKitAppsGetAppInstalledVersionIdContext
        _getAppInstalledVersionIdCtx;

    private void BeginGetAppInstalledVersionId()
    {
        _getAppInstalledVersionIdCtx?.Dispose();
        _getAppInstalledVersionIdCtx =
            LibPatchKitApps.GetAppInstalledVersionId(path: State.AppState.Path);
    }

    private void UpdateGetAppInstalledVersionId()
    {
        if (_getAppInstalledVersionIdCtx == null ||
            _getAppInstalledVersionIdCtx.IsExecuting)
        {
            return;
        }

        if (_getAppInstalledVersionIdCtx.Error ==
            LibPatchKitAppsGetAppInstalledVersionIdError.None)
        {
            State.AppState.LatestVersionId =
                _getAppInstalledVersionIdCtx.Result;
            _hasStateChanged = true;
        }

        _getAppInstalledVersionIdCtx?.Dispose();
        _getAppInstalledVersionIdCtx = null;
    }

    private LibPatchKitAppsGetAppLatestVersionIdContext
        _getAppLatestVersionIdCtx;

    private void BeginGetAppLatestVersionId()
    {
        if (_overrideAppLatestVersionId.HasValue)
        {
            State.AppState.LatestVersionId = _overrideAppLatestVersionId;
            return;
        }

        _getAppLatestVersionIdCtx?.Dispose();
        _getAppLatestVersionIdCtx =
            LibPatchKitApps.GetAppLatestVersionId(
                secret: State.AppState.Secret);
    }

    private void UpdateGetAppLatestVersionId()
    {
        if (_getAppLatestVersionIdCtx == null ||
            _getAppLatestVersionIdCtx.IsExecuting)
        {
            return;
        }

        if (_getAppLatestVersionIdCtx.Error ==
            LibPatchKitAppsGetAppLatestVersionIdError.None)
        {
            State.AppState.LatestVersionId = _getAppLatestVersionIdCtx.Result;
            _hasStateChanged = true;
        }

        _getAppLatestVersionIdCtx?.Dispose();
        _getAppLatestVersionIdCtx = null;
    }

    private LibPatchKitAppsGetAppInfoContext _getAppInfoContext;

    private void BeginGetAppInfo()
    {
        _getAppInfoContext?.Dispose();
        _getAppInfoContext =
            LibPatchKitApps.GetAppInfo(secret: State.AppState.Secret);
    }

    private void UpdateGetAppInfo()
    {
        if (_getAppInfoContext == null || _getAppInfoContext.IsExecuting)
        {
            return;
        }

        if (_getAppInfoContext.Error == LibPatchKitAppsGetAppInfoError.None)
        {
            State.AppState.Info = _getAppInfoContext.Result;
            _hasStateChanged = true;
        }

        _getAppInfoContext?.Dispose();
        _getAppInfoContext = null;
    }

    private LibPatchKitAppsGetAppVersionListContext _getAppVersionListContext;

    private void BeginGetAppVersionList()
    {
        _getAppVersionListContext?.Dispose();
        _getAppVersionListContext =
            LibPatchKitApps.GetAppVersionList(secret: State.AppState.Secret);
    }

    private void UpdateGetAppVersionList()
    {
        if (_getAppVersionListContext == null ||
            _getAppVersionListContext.IsExecuting)
        {
            return;
        }

        if (_getAppVersionListContext.Error ==
            LibPatchKitAppsGetAppVersionListError.None)
        {
            State.AppState.Versions = _getAppVersionListContext.Result;
            _hasStateChanged = true;
        }

        _getAppVersionListContext?.Dispose();
        _getAppVersionListContext = null;
    }
}
}