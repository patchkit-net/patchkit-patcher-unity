namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public interface ITorrentClientFactory
    {
        ITorrentClient Create();
    }
}