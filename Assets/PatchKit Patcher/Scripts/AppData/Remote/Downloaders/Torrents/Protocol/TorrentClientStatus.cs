using Newtonsoft.Json;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents.Protocol
{
    public class TorrentClientStatus
    {
        [JsonProperty("data")]
        public TorrentClientStatusData Data { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}