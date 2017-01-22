using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public interface IBaseHttpDownloader
    {
        event DataAvailableHandler DataAvailable;

        void SetBytesRange(long start, long end = -1L);

        void Download(CancellationToken cancellationToken);
    }
}