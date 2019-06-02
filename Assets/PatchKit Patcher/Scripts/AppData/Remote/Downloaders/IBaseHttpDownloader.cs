using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Base HTTP downloader.
    /// </summary>
    public interface IBaseHttpDownloader
    {
        event DataAvailableHandler DataAvailable;

        void SetBytesRange(BytesRange? range);

        void Download(CancellationToken cancellationToken);
    }
}