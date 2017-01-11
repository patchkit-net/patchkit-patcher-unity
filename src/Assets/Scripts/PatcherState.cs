namespace PatchKit.Unity.Patcher
{
    public enum PatcherState
    {
        None,
        Preparing,
        UpdatingApp,
        StartingApp,
        WaitingForUserDecision
    }
}