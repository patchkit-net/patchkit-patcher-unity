using Newtonsoft.Json;

namespace PatchKit.Api.Models.Keys
{
    public struct Error
    {
        /// <summary>
        /// Human-readable error message
        /// </summary>
        [JsonProperty("message")]
        public string Message;
        
    }
}
