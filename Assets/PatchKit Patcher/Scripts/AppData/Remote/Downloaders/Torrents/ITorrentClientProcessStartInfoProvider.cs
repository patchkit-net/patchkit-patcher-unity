using System;
using System.Diagnostics;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents
{
    [Obsolete("Torrent downloading is deprecated.")]
    public interface ITorrentClientProcessStartInfoProvider
    {
        ProcessStartInfo GetProcessStartInfo();
    }
}
