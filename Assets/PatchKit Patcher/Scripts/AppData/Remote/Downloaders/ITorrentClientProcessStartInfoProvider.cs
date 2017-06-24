using System.Diagnostics;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public interface ITorrentClientProcessStartInfoProvider
    {
        ProcessStartInfo GetProcessStartInfo();
    }
}
