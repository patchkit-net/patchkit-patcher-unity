using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
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

    private struct InitializationData
    {
        [NotNull]
        public string AppSecret;

        [NotNull]
        public string AppPath;

        public string LockFilePath;

        public int? OverrideAppLatestVersionId;

        public bool? IsOnline;
    }

    private InitializationData? GetEditorInitializationData()
    {
        Assert.IsNotNull(value: Application.dataPath);
        Assert.IsNotNull(value: EditorAppSecret);

        return new InitializationData
        {
            AppPath = Application.dataPath.Replace(
                oldValue: "/Assets",
                newValue: $"/Temp/PatcherApp{EditorAppSecret}"),
            AppSecret = EditorAppSecret,
            LockFilePath = null,
            OverrideAppLatestVersionId = EditorOverrideAppLatestVersionId > 0
                ? (int?) EditorOverrideAppLatestVersionId
                : null,
            IsOnline = null
        };
    }

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

    private void Initialize()
    {
        _instance = this;
        Assert.raiseExceptions = true;
        Application.runInBackground = true;

        InitializeLibPatchKitApps();

#if UNITY_EDITOR
        var data = GetEditorInitializationData();
#else
        var data = GetCommandLineInitializationData();
#endif

        if (!data.HasValue)
        {
            _state = new PatcherState
            {
                Kind = PatcherStateKind.DisplayingError,
                Error = PatcherError.NoLauncherError,
                HasChanged = true
            };

            return;
        }

        _state = new PatcherState(
            appSecret: data.Value.AppSecret,
            appPath: data.Value.AppPath,
            lockFilePath: data.Value.LockFilePath,
            overrideAppLatestVersionId: data.Value.OverrideAppLatestVersionId)
        {
            Kind = PatcherStateKind.Initializing,
            IsOnline = data.Value.IsOnline ?? true,
            AppState =
            {
                ShouldBeUpdatedAutomatically = AutomaticallyUpdateApp,
                ShouldBeStartedAutomatically = AutomaticallyStartApp
            },
            HasChanged = true
        };

        SafeFinishInitialization();
    }

    private async void SafeFinishInitialization()
    {
        await SafeInvoke(func: FinishInitialization);
    }

    private LibPatchKitAppsFileLock _fileLock;

    private async Task FinishInitialization()
    {
        Assert.IsNotNull(value: State.AppState);

        try
        {
            if (!string.IsNullOrEmpty(value: State.LockFilePath))
            {
                // ReSharper disable once PossibleNullReferenceException
                _fileLock = await LibPatchKitApps.GetFileLockAsync(
                    path: State.LockFilePath,
                    cancellationToken: CancellationToken.None);
            }
        }
        catch (LibPatchKitAppsFileAlreadyInUseException)
        {
            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.DisplayingError;
                    State.Error = PatcherError.MultipleInstancesError;
                });
            return;
        }
        catch (LibPatchKitAppsNotExistingFileException)
        {
        }

        var updates = Task.WhenAll(
            FetchAppInfo(),
            FetchAppVersions(),
            FetchAppLatestVersionId(),
            FetchAppInstalledVersionId());

        Assert.IsNotNull(value: updates);

        if (State.AppState.ShouldBeUpdatedAutomatically)
        {
            await UpdateApp();
        }

        if (State.AppState.ShouldBeStartedAutomatically)
        {
            await StartApp();
        }

        ModifyState(x: () => State.Kind = PatcherStateKind.Idle);

        await updates;
    }
}