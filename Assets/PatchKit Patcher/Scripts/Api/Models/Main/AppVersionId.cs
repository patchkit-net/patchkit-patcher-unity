using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct AppVersionId
    {
        /// <summary>
        /// Version id.
        /// </summary>
        [JsonProperty("id")]
        public int Id;
        
    }
}
