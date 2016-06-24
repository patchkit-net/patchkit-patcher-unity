namespace PatchKit.Unity.Patcher
{
    public enum PatcherState
    {
        None,
        Patching,
        Succeed,
        Cancelled,
        NoInternetConnection,
        Failed
    }
}