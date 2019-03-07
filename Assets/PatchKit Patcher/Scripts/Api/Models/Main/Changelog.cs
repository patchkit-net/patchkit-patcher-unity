using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct Changelog
    {
        /// <summary>
        /// Version id.
        /// </summary>
        [JsonProperty("version_id")]
        public int VersionId;
        
        /// <summary>
        /// Human readable label.
        /// </summary>
        [JsonProperty("version_label")]
        public string VersionLabel;
        
        /// <summary>
        /// Changes description.
        /// </summary>
        [JsonProperty("changes")]
        public string Changes;
        
    }
}
