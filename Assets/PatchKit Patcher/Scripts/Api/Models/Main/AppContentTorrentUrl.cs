using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct AppContentTorrentUrl
    {
        /// <summary>
        /// Url to content torrent file.
        /// </summary>
        [JsonProperty("url")]
        public string Url;
        
    }
}
