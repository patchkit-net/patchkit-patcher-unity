namespace PatchKit.Unity.Patcher
{
    public enum PatcherState
    {
        None,
        LoadingPatcherConfiguration,
        UpdatingApp,
        StartingApp,
        HandlingErrorMessage,
        WaitingForUserDecision
    }
}