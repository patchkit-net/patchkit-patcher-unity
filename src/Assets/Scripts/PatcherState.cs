namespace PatchKit.Unity.Patcher
{
    public enum PatcherState
    {
        None,
        CheckingAppStatus,
        UpdatingApp,
        StartingApp,
        WaitingForUserDecision
    }
}