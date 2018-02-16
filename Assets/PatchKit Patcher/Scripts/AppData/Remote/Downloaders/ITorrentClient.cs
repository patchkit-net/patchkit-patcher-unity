using System;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents.Protocol;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public interface ITorrentClient : IDisposable
    {
        void AddTorrent(string torrentFilePath, string downloadDirectoryPath, CancellationToken cancellationToken);

        TorrentClientStatus GetStatus(CancellationToken cancellationToken);
    }
}