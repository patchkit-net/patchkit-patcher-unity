using System;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents
{
    [Obsolete("Torrent downloading is deprecated.")]
    public interface ITorrentDownloader
    {
        event DownloadProgressChangedHandler DownloadProgressChanged;

        void Download(CancellationToken cancellationToken);
    }
}