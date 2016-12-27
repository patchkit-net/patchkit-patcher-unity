namespace PatchKit.Unity.Patcher.Progress
{
    internal class DownloadProgress : IDownloadProgress
    {
        public DownloadProgress(double weight)
        {
            Weight = weight;
        }

        public double Weight { get; private set; }

        public double Value { get; set; }

        public long Bytes { get; set; }

        public long TotalBytes { get; set; }

        public double DownloadSpeed { get; set; }
    }
}