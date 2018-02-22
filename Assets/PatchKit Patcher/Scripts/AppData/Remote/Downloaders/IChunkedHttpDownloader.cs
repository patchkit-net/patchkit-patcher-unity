using System;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Network;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    //TODO: Rename to IChunkedFileHttpDownloader
    public interface IChunkedHttpDownloader
    {
        event DownloadProgressChangedHandler DownloadProgressChanged;

        void Download(CancellationToken cancellationToken);
        void SetRange(BytesRange range);
    }
}