namespace PatchKit.Unity.Patcher
{
    public enum PatcherState
    {
        None,
        LoadingPatcherData,
        LoadingPatcherConfiguration,
        UpdatingApp,
        StartingApp,
        HandlingErrorMessage,
        WaitingForUserDecision,
    }
}