using PatchKit.Unity.Patcher.Zip;

namespace PatchKit.Unity.Patcher.Net
{
    public struct DownloadProgress : ICustomProgress
    {
        public long DownloadedBytes { get; set; }

        public long TotalBytes { get; set; }

        public double KilobytesPerSecond { get; set; }

        public double Progress { get; set; }
    }
}