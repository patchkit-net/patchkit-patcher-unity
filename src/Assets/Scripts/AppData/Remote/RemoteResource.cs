namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public struct RemoteResource
    {
        public string[] TorrentUrls;

        public string[] Urls;

        public long Size;

        public string HashCode;

        public ChunksData ChunksData;
    }
}