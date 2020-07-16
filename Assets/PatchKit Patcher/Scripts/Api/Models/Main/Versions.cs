using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct Versions
    {
        /// <summary>
        /// Version id.
        /// </summary>
        [JsonProperty("vid")]
        public int VersionId;
        
        /// <summary>
        /// Human readable label.
        /// </summary>
        [JsonProperty("label")]
        public string VersionLabel;
        
        /// <summary>
        /// Changes description.
        /// </summary>
        [JsonProperty("changelog")]
        public string Changes;
        
        /// <summary>
        /// Unix timestamp of publish date.
        /// </summary>
        [JsonProperty("publish_time")]
        public long PublishTime;
        
        /// <summary>
        /// Text publish date.
        /// </summary>
        [JsonProperty("publish_date")]
        public string PublishDate;
    }
}
