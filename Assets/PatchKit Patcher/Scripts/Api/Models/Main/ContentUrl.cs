using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct ContentUrl
    {
        /// <summary>
        /// Url to content file.
        /// </summary>
        [JsonProperty("url")]
        public string Url;
        
        /// <summary>
        /// Url to meta file if available.
        /// </summary>
        [JsonProperty("meta_url")]
        public string MetaUrl;
        
        /// <summary>
        /// Region name of this mirror server.
        /// </summary>
        [JsonProperty("region")]
        public string Region;
        
        /// <summary>
        /// Value of recent server load (usage). Servers with lower load should be prioritorized.
        /// </summary>
        [JsonProperty("load")]
        public double Load;
        
    }
}
