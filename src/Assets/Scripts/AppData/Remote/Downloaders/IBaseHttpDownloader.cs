using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Base HTTP downloader.
    /// </summary>
    public interface IBaseHttpDownloader
    {
        event DataAvailableHandler DataAvailable;

        void SetBytesRange(long bytesRangeStart, long bytesRangeEnd = -1L);

        void Download(CancellationToken cancellationToken);
    }
}