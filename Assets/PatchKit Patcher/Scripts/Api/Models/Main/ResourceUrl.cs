using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct ResourceUrl
    {
        /// <summary>
        /// Url to diff file.
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
        
        /// <summary>
        /// Size of single file part. If set to 0 then file is distributed within a single part (default). Part URLs can be evaluated from first part URL - second and following parts have '.INDEX' names. For instance ../c49fde98-a07c-11e7-8919-33f7211dd1c0 is first part, then ../c49fde98-a07c-11e7-8919-33f7211dd1c0.1 is second and ../c49fde98-a07c-11e7-8919-33f7211dd1c0.2 is third.
        /// </summary>
        [JsonProperty("part_size")]
        public long PartSize;
        
    }
}
