namespace PatchKit.Unity.Patcher.Status
{
    public class DownloadStatusHolder : IStatusHolder
    {
        public long Bytes;

        public long TotalBytes;

        public double Speed;

        public bool IsDownloading;

        public double Weight { get; private set; }

        public double Progress
        {
            get { return Bytes/(double) TotalBytes; }
        }

        public DownloadStatusHolder(double weight)
        {
            Weight = weight;
        }
    }
}