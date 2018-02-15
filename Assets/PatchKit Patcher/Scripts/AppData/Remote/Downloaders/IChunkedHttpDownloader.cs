using System;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Network;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public interface IChunkedHttpDownloader
    {
        event DownloadProgressChangedHandler DownloadProgressChanged;

        void Download(CancellationToken cancellationToken);
        void SetRange(BytesRange range);
    }
}