namespace PatchKit.Unity.Patcher.Progress
{
    internal interface IDownloadProgress : IProgress
    {
        long Bytes { get; }

        long TotalBytes { get; }

        double DownloadSpeed { get; }
    }
}