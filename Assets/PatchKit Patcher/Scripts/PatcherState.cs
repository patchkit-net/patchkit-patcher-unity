namespace PatchKit_Patcher.Scripts
{
    public struct PatcherState
    {
        public PatcherStateKind Kind { get; }

        public PatcherUpdateAppState? UpdateAppState { get; }

        public PatcherAppState AppState { get; }
    }
}