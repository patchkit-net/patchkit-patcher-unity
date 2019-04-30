using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher : MonoBehaviour
{
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

    public bool AutomaticallyStartApp;

    public bool AutomaticallyUpdateApp = true;

    private async void Awake()
    {
        await SafeInvoke(func: Initialize);

        var updates = Task.WhenAll(
            SafeInvoke(func: FetchAppInfo),
            SafeInvoke(func: FetchAppVersions),
            SafeInvoke(func: FetchAppLatestVersionId),
            SafeInvoke(func: FetchAppInstalledVersionId));

        Assert.IsNotNull(value: updates);

        if (AutomaticallyUpdateApp && State.IsOnline)
        {
            Debug.Log(message: "Automatically updating app.");

            await SafeInvoke(func: UpdateApp);
        }

        if (AutomaticallyStartApp)
        {
            Debug.Log(message: "Automatically starting app.");

            await SafeInvoke(func: StartApp);
        }
        else
        {
            ModifyState(x: () => State.Kind = PatcherStateKind.Idle);
        }

        await updates;
    }

    public async void OnStartAppRequested()
    {
        if (State.Kind != PatcherStateKind.Idle)
        {
            return;
        }

        await SafeInvoke(func: StartApp);
    }

    public async void OnUpdateAppRequested()
    {
        if (State.Kind != PatcherStateKind.Idle)
        {
            return;
        }

        await SafeInvoke(func: UpdateApp);
    }

    public async void OnCancelUpdateAppRequested()
    {
        if (State.Kind != PatcherStateKind.UpdatingApp)
        {
            return;
        }

        await SafeInvoke(func: CancelUpdateApp2);
    }

    public async void OnSetLicenseKeyAndUpdateAppRequested(
        [NotNull] string licenseKey)
    {
        if (State.Kind != PatcherStateKind.AskingForAppLicenseKey)
        {
            return;
        }

        await SafeInvoke(
            func: () => SetLicenseKeyAndUpdateApp(licenseKey: licenseKey));
    }

    public async void OnCancelSettingLicenseKeyRequested()
    {
        if (State.Kind != PatcherStateKind.AskingForAppLicenseKey)
        {
            return;
        }

        await SafeInvoke(func: CancelSettingLicenseKey);
    }

    public async void OnQuitRequested()
    {
        await SafeInvoke(func: Quit2);
    }

    public async void OnAcceptErrorRequested()
    {
        if (State.Kind != PatcherStateKind.DisplayingError)
        {
            return;
        }

        await SafeInvoke(func: AcceptError);
    }

    //TODO: Remove after moving to Legacy
    public void Quit()
    {
        OnQuitRequested();
    }

    //TODO: Remove after moving to Legacy
    public void CancelUpdateApp()
    {
        OnCancelUpdateAppRequested();
    }

    private void Update()
    {
        UpdateState();
    }

    private void OnDestroy()
    {
        State.FileLock?.Dispose();
        State.FileLock = null;
    }

    private async Task SafeInvoke([NotNull] Func<Task> func)
    {
        try
        {
            var t = func();
            Assert.IsNotNull(value: t);
            await t;
        }
        catch (LibPatchKitAppsUnauthorizedAccessException)
        {
            Debug.Log(message: "Unauthorized access.");

            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.DisplayingError;
                    State.Error = PatcherError.UnauthorizedAccessError;
                });
        }
        catch (Exception e)
        {
            Debug.LogException(exception: e);

            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.DisplayingError;
                    State.Error = PatcherError.InternalError;
                });
        }
    }
}