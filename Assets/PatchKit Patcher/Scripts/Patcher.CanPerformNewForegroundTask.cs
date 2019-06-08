using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

public partial class Patcher
{
    private bool CanPerformNewForegroundTask()
    {
        return CanPerformNewTask() &&
            !_hasInitializeTask &&
            !_hasAppUpdateTask &&
            !_hasAppStartTask &&
            !_hasRestartWithHigherPermissionsTask &&
            !_hasRestartWithLauncherTask;
    }
}