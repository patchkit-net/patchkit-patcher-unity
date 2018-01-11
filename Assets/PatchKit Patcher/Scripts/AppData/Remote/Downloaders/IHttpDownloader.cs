using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public interface IHttpDownloader
    {
        event DownloadProgressChangedHandler DownloadProgressChanged;

        void Download(CancellationToken cancellationToken);
    }
}