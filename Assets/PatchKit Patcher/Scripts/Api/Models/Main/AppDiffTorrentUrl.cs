using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct AppDiffTorrentUrl
    {
        /// <summary>
        /// Url to diff torrent file.
        /// </summary>
        [JsonProperty("url")]
        public string Url;
        
    }
}
