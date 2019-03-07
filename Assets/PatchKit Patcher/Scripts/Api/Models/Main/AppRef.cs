using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct AppRef
    {
        [JsonProperty("secret")]
        public string Secret;
        
    }
}
