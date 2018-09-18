using System.Diagnostics;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents
{
    public interface ITorrentClientProcessStartInfoProvider
    {
        ProcessStartInfo GetProcessStartInfo();
    }
}
