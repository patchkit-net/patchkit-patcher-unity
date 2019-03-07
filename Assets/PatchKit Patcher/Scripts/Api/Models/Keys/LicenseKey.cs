using Newtonsoft.Json;

namespace PatchKit.Api.Models.Keys
{
    public struct LicenseKey
    {
        [JsonProperty("key")]
        public string Key;
        
        [JsonProperty("app_secret")]
        public string AppSecret;
        
        [JsonProperty("key_secret")]
        public string KeySecret;
        
        [JsonProperty("collection_id")]
        public int CollectionId;
        
        /// <summary>
        /// Number of key registrations. This is a request wihout a app_secret.
        /// </summary>
        [JsonProperty("registrations")]
        public int Registrations;
        
        /// <summary>
        /// If set to true, this key is blocked for further use.
        /// </summary>
        [JsonProperty("blocked")]
        public bool Blocked;
        
    }
}
