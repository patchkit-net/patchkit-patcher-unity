using Newtonsoft.Json;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents.Protocol
{
    public class TorrentClientMessage
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}