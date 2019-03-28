using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct AppDiffSummary
    {
        /// <summary>
        /// Version string. Format: MAJOR.MINOR. Present in >= 2.4
        /// </summary>
        [JsonProperty("version")]
        public string Version;
        
        /// <summary>
        /// Diff size.
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
        /// List of added files.
        /// </summary>
        [JsonProperty("added_files")]
        public string[] AddedFiles;
        
        /// <summary>
        /// List of modified files.
        /// </summary>
        [JsonProperty("modified_files")]
        public string[] ModifiedFiles;
        
        /// <summary>
        /// List of removed files.
        /// </summary>
        [JsonProperty("removed_files")]
        public string[] RemovedFiles;
        
        [JsonProperty("hash_code")]
        public string HashCode;
        
        [JsonProperty("chunks")]
        public Chunks Chunks;
        
    }
}
