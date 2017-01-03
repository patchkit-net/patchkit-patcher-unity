namespace PatchKit.Unity.Patcher.Status
{
    internal interface IDownloadStatusReporter
    {
        void OnDownloadStarted();

        void OnDownloadProgressChanged(long bytes, long totalBytes);

        void OnDownloadEnded();
    }
}