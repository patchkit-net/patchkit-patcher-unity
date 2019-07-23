public partial class Patcher
{
    private bool CanPerformNewForegroundTask()
    {
        return CanPerformNewTask() &&
            (!_hasApp || (!_hasAppStartTask && !_hasAppUpdateTask));
    }
}