using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct Plan
    {
        [JsonProperty("capabilities")]
        public string[] Capabilities;
        
    }
}
