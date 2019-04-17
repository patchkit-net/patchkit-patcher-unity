using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Standart.Hash.xxHash;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher : MonoBehaviour
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

    public bool AutomaticallyStartApp;

    public bool AutomaticallyUpdateApp = true;

#if UNITY_EDITOR
    public string EditorAppSecret;
    public int EditorOverrideAppLatestVersionId;
#endif

    private void Awake()
    {
        _instance = this;

        Initialize();
    }

    private async void Start()
    {
        await SafeInvoke(func: Startup);
    }

    private void Update()
    {
        UpdateState();
    }

    private void OnDestroy()
    {
        Dispose();
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
            ModifyState(
                x: () => State.Kind = PatcherStateKind.DisplayingInternalError);
        }
        // ReSharper disable once RedundantCatchClause
        catch (LibPatchKitAppsUnauthorizedAccessException)
        {
            ModifyState(
                x: () =>
                    State.Kind = PatcherStateKind
                        .DisplayingUnauthorizedAccessError);
        }
    }
}