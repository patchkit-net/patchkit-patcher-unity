using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Base HTTP downloader.
    /// </summary>
    public interface IBaseHttpDownloader
    {
        void Download(string url, BytesRange? bytesRange, int timeout, DataAvailableHandler onDataAvailable, CancellationToken cancellationToken);
    }
}