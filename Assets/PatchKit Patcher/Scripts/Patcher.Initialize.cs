using System;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities;

public partial class Patcher
{
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

    private void Initialize()
    {
        Assert.raiseExceptions = true;
        Application.runInBackground = true;
        UnityDispatcher.Initialize();

        InitializeLibPatchKitApps();
        InitializeState();
    }
}