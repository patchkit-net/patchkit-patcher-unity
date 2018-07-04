namespace PatchKit.Unity.Patcher
{
    public enum PatcherState
    {
        None,
        Connecting,
        LoadingPatcherData,
        LoadingPatcherConfiguration,
        WaitingForUserDecision,
        UpdatingApp,
        StartingApp,
        DisplayingError,
    }
}