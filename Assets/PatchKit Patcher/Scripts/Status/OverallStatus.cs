namespace PatchKit.Unity.Patcher.Status
{
    public struct OverallStatus
    {
        public double Progress;

        public bool IsDownloading;

        public double DownloadBytesPerSecond;

        public long DownloadBytes;

        public long DownloadTotalBytes;

        public double DownloadProgress
        {
            get
            {
                if (DownloadTotalBytes == 0)
                {
                    return 0.0;
                }

                return DownloadBytes/(double) DownloadTotalBytes;
            }
        }

        public string Description;
    }
}