using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public struct RemoteResource
    {
        public string[] TorrentUrls;

        public string[] Urls;

        public string[] MetaUrls;

        public long Size;

        public string HashCode;

        public ChunksData ChunksData;

        public override string ToString()
        {
            return "urls: {" + string.Join(", ", Urls) + "}\n" +
                   "torrent urls: {" + string.Join(", ", TorrentUrls) + "}\n" +
                   "size: " + Size + "\n" +
                   "hashcode: " + HashCode;
        }

        public bool HasMetaUrls()
        {
            return MetaUrls != null && MetaUrls.Length > 0 && MetaUrls[0].Length > 0;
        }
    }
}