namespace PatchKit.Unity.Patcher.Progress
{
    internal interface IDownloadProgressReporter
    {
        void OnDownloadProgressChanged(long bytes, long totalBytes);
    }
}