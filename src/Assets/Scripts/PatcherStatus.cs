namespace PatchKit.Unity.Patcher
{
    public struct PatcherStatus
    {
        public PatcherState State;

        public float Progress;

        public bool IsDownloading;

        public float DownloadProgress;

        public float DownloadSpeed;
    }
}
