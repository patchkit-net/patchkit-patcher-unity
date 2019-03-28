using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct AppContentSummaryFile
    {
        /// <summary>
        /// File path.
        /// </summary>
        [JsonProperty("path")]
        public string Path;
        
        /// <summary>
        /// File hash.
        /// </summary>
        [JsonProperty("hash")]
        public string Hash;
        
        /// <summary>
        /// Uncompressed file size in bytes. Present in >= 2.3
        /// </summary>
        [JsonProperty("size")]
        public long Size;
        
        /// <summary>
        /// File flags, present in >= 2.3
        /// </summary>
        [JsonProperty("flags")]
        public string Flags;
        
    }
}
