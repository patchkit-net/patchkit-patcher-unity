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

    private void InitializeLibPatchKitApps()
    {
        Debug.Log(message: "Initializing libpkapps...");

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

        Debug.Log(message: "libpkapps initialized.");
    }

    private void Initialize()
    {
        Debug.Log(message: "Initializing patcher...");

        _instance = this;
        Assert.raiseExceptions = true;
        Application.runInBackground = true;

        InitializeLibPatchKitApps();

#if UNITY_EDITOR
        var data = LoadEditorInitializationData();
#else
        var data = LoadCommandLineInitializationData();
#endif

        if (!data.HasValue)
        {
            Debug.Log(message: "Initialization data wasn't loaded.");
            Debug.Log(
                message:
                "Creating state with kind: DisplayingError (NoLauncherError).");

            _state = new PatcherState
            {
                Kind = PatcherStateKind.DisplayingError,
                Error = PatcherError.NoLauncherError,
                HasChanged = true
            };

            Debug.Log(message: "Initializing patcher finished.");

            return;
        }

        Debug.Log(
            message: $"InitializationData.AppPath = {data.Value.AppPath}");
        Debug.Log(
            message: $"InitializationData.AppSecret = {data.Value.AppSecret}");
        Debug.Log(
            message:
            $"InitializationData.IsOnline = {data.Value.IsOnline?.ToString() ?? "null"}");
        Debug.Log(
            message:
            $"InitializationData.LockFilePath = {data.Value.LockFilePath ?? "null"}");
        Debug.Log(
            message:
            $"InitializationData.OverrideAppLatestVersionId = {data.Value.OverrideAppLatestVersionId?.ToString() ?? "null"}");

        Debug.Log(message: "Creating state with kind: Initializing.");

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
        Debug.Log(message: "Finishing initialization...");

        Assert.IsNotNull(value: State.AppState);
        Assert.IsTrue(condition: State.Kind == PatcherStateKind.Initializing);

        try
        {
            if (!string.IsNullOrEmpty(value: State.LockFilePath))
            {
                Debug.Log(
                    message: $"Getting file lock on '{State.LockFilePath}'...");

                // ReSharper disable once PossibleNullReferenceException
                _fileLock = await LibPatchKitApps.GetFileLockAsync(
                    path: State.LockFilePath,
                    cancellationToken: CancellationToken.None);

                Debug.Log(message: "File lock acquired.");
            }
        }
        catch (LibPatchKitAppsFileAlreadyInUseException)
        {
            Debug.Log(message: "Failed to get file lock: already in use.");

            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.DisplayingError;
                    State.Error = PatcherError.MultipleInstancesError;
                });

            Debug.Log(
                message:
                "Initialization finished with MultipleInstancesError.");

            return;
        }
        catch (LibPatchKitAppsNotExistingFileException)
        {
            Debug.Log(message: "Failed to get file lock: file doesn't exist.");
        }

        var updates = Task.WhenAll(
            FetchAppInfo(),
            FetchAppVersions(),
            FetchAppLatestVersionId(),
            FetchAppInstalledVersionId());

        Assert.IsNotNull(value: updates);

        if (State.AppState.ShouldBeUpdatedAutomatically && State.IsOnline)
        {
            Debug.Log(message: "Automatically updating app.");

            await UpdateApp();
        }

        if (State.AppState.ShouldBeStartedAutomatically)
        {
            Debug.Log(message: "Automatically starting app.");

            await StartApp();
        }

        ModifyState(x: () => State.Kind = PatcherStateKind.Idle);

        await updates;

        Debug.Log(message: "Initialization finished.");
    }
}