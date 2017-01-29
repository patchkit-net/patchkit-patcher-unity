namespace PatchKit.Unity.Patcher.Status
{
    public class DownloadStatusHolder : IStatusHolder
    {
        public long Bytes;

        public long TotalBytes;

        public double BytesPerSecond;

        public bool IsDownloading;

        public double Weight { get; private set; }

        public double Progress
        {
            get
            {
                if (TotalBytes == 0)
                {
                    return 0.0;
                }

                return Bytes/(double) TotalBytes;
            }
        }

        public DownloadStatusHolder(double weight)
        {
            Weight = weight;
        }
    }
}