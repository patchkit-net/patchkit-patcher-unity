namespace PatchKit.Unity.Patcher.Progress
{
    public interface IDownloadProgress
    {
        void OnDownloadProgress(long bytes, long totalBytes);
    }
}