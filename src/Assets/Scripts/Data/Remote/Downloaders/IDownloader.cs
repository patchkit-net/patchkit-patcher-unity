using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.Data.Remote.Downloaders
{
    internal interface IDownloader
    {
        void Download(CancellationToken cancellationToken);
    }
}