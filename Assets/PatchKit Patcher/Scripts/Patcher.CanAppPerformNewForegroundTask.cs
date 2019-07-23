public partial class Patcher
{
    private bool CanAppPerformNewForegroundTask()
    {
        return _hasApp &&
            CanPerformNewForegroundTask();
    }
}