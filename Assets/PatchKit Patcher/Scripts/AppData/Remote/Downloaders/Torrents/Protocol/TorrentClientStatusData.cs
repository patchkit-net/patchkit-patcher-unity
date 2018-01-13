using Newtonsoft.Json;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents.Protocol
{
    public class TorrentClientStatusData
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("torrents")]
        public TorrentStatus[] Torrents { get; set; }
    }
}