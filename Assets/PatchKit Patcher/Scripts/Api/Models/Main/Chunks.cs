using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct Chunks
    {
        [JsonProperty("size")]
        public int Size;
        
        [JsonProperty("hashes")]
        public string[] Hashes;
        
    }
}
