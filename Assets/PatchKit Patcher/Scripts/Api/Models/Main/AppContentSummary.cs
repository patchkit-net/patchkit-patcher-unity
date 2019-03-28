using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct AppContentSummary
    {
        /// <summary>
        /// Version string. Format: MAJOR.MINOR. Present in >= 2.4
        /// </summary>
        [JsonProperty("version")]
        public string Version;
        
        /// <summary>
        /// Content size.
        /// </summary>
        [JsonProperty("size")]
        public long Size;
        
        /// <summary>
        /// Uncompressed archive size. Present in >= 2.4.
        /// </summary>
        [JsonProperty("uncompressed_size")]
        public long UncompressedSize;
        
        /// <summary>
        /// Encryption method.
        /// </summary>
        [JsonProperty("encryption_method")]
        public string EncryptionMethod;
        
        /// <summary>
        /// Compression method.
        /// </summary>
        [JsonProperty("compression_method")]
        public string CompressionMethod;
        
        /// <summary>
        /// List of content files.
        /// </summary>
        [JsonProperty("files")]
        public AppContentSummaryFile[] Files;
        
        [JsonProperty("hash_code")]
        public string HashCode;
        
        [JsonProperty("chunks")]
        public Chunks Chunks;
        
    }
}
