namespace PatchKit.Patching.Unity
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