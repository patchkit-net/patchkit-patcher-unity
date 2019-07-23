public partial class Patcher
{
    private bool CanAppPerformNewTask()
    {
        return _hasApp &&
            CanPerformNewTask();
    }
}