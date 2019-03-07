using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct AppContentUrl
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
        /// ISO code of origin country.
        /// </summary>
        [JsonProperty("country")]
        public string Country;
        
    }
}
