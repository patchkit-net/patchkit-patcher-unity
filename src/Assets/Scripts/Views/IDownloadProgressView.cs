namespace PatchKit.Unity.Patcher.Views
{
    public interface IDownloadProgressView : IView
    {
        void UpdateDownloadProgress(long downloadedBytes, long totalBytes);
    }
}