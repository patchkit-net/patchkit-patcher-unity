namespace PatchKit.Unity.Patcher.Status
{
    public interface IDownloadStatusReporter
    {
        void OnDownloadStarted();

        void OnDownloadProgressChanged(long bytes, long totalBytes);

        void OnDownloadEnded();
    }
}