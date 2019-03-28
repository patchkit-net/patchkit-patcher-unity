using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct Error
    {
        /// <summary>
        /// Human-readable error message
        /// </summary>
        [JsonProperty("message")]
        public string Message;
        
        /// <summary>
        /// Error symbol
        /// </summary>
        [JsonProperty("symbol")]
        public string Symbol;
        
    }
}
