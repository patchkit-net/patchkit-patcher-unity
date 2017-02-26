namespace PatchKit.Unity.Patcher
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