namespace PatchKit.Unity.Patcher
{
    public enum PatcherState
    {
        None,
        CheckingInternetConnection,
        LoadingPatcherConfiguration,
        UpdatingApp,
        StartingApp,
        HandlingErrorMessage,
        WaitingForUserDecision
    }
}