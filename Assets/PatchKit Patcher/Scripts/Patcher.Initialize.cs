using System;
using System.Threading;
using System.Threading.Tasks;
using Debugging;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task Initialize()
    {
        Debug.Log(message: "Initializing patcher...");
        Debug.Log(message: $"Patcher version: {PatcherVersion.Text}");
        Debug.Log(
            message: $"Runtime version: {EnvironmentInfo.GetRuntimeVersion()}");
        Debug.Log(
            message: $"System version: {EnvironmentInfo.GetSystemVersion()}");
        Debug.Log(
            message:
            $"System information: {EnvironmentInfo.GetSystemInformation()}");

        InitializeLibPatchKitApps();

#if UNITY_EDITOR
        var data = LoadEditorInitializationData();
#else
        var data = LoadCommandLineInitializationData();
#endif

        if (!data.HasValue)
        {
            Debug.Log(message: "Initialization data wasn't loaded.");

            _state = new PatcherState();
            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.DisplayingError;
                    State.Error = PatcherError.NoLauncherError;
                });

            Debug.Log(
                message: "Initializing patcher finished with NoLauncherError.");

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

        _state = new PatcherState(
            appSecret: data.Value.AppSecret,
            appPath: data.Value.AppPath,
            lockFilePath: data.Value.LockFilePath,
            overrideAppLatestVersionId: data.Value.OverrideAppLatestVersionId);

        ModifyState(
            x: () =>
            {
                State.Kind = PatcherStateKind.Initializing;
                State.IsOnline = data.Value.IsOnline ?? true;
            });

        try
        {
            if (!string.IsNullOrEmpty(value: State.LockFilePath))
            {
                Debug.Log(
                    message: $"Getting file lock on '{State.LockFilePath}'...");

                // ReSharper disable once PossibleNullReferenceException
                State.FileLock = await LibPatchKitApps.GetFileLockAsync(
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

        ModifyState(x: () => State.Kind = PatcherStateKind.Idle);

        Debug.Log(message: "Initialization finished.");
    }

    private static void InitializeLibPatchKitApps()
    {
        //TODO: Override API with environment variables
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
}