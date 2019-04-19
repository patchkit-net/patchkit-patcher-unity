using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher : MonoBehaviour
{
    public bool AutomaticallyStartApp;

    public bool AutomaticallyUpdateApp = true;

    private void Awake()
    {
        Initialize();
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
        catch (LibPatchKitAppsInternalErrorException)
        {
            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.DisplayingError;
                    State.Error = PatcherError.InternalError;
                });
        }
        catch (LibPatchKitAppsUnauthorizedAccessException)
        {
            ModifyState(
                x: () =>
                {
                    State.Kind = PatcherStateKind.DisplayingError;
                    State.Error = PatcherError.UnauthorizedAccessError;
                });
        }
    }
}