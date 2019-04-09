namespace PatchKit.Unity.Patcher
{
    public enum PatcherStateKindOld
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