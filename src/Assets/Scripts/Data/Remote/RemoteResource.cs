namespace PatchKit.Unity.Patcher.Data.Remote
{
    public struct RemoteResource
    {
        public string[] TorrentUrls;

        public string[] ContentUrls;

        public long ContentSize;

        public ChunksData ChunksData;
    }
}