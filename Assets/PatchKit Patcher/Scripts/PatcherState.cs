namespace PatchKit.Patching.Unity
{
    public enum PatcherState
    {
        None,
        LoadingPatcherData,
        LoadingPatcherConfiguration,
        WaitingForUserDecision,
        UpdatingApp,
        StartingApp
    }
}