namespace PatchKit_Patcher.Scripts
{
    public struct PatcherUpdateAppState
    {
        public long InstalledBytes { get; }

        public long TotalBytes { get; }

        public double BytesPerSecond { get; }
    }
}