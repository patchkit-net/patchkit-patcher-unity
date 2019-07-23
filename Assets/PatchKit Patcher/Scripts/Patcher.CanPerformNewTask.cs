public partial class Patcher
{
    private bool CanPerformNewTask()
    {
        return _isInitialized &&
            !_hasInitializeTask &&
            !_hasRestartWithHigherPermissionsTask &&
            !_hasRestartWithLauncherTask &&
            !_hasQuit &&
            !_hasQuitTask;
    }
}