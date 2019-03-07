using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct PatcherBannerImageDimensions
    {
        [JsonProperty("x")]
        public int X;
        
        [JsonProperty("y")]
        public int Y;
        
    }
}
