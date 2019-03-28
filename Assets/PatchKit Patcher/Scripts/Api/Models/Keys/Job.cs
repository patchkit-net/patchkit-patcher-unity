using Newtonsoft.Json;

namespace PatchKit.Api.Models.Keys
{
    public struct Job
    {
        [JsonProperty("guid")]
        public string Guid;
        
        [JsonProperty("pending")]
        public bool Pending;
        
        [JsonProperty("finished")]
        public bool Finished;
        
    }
}
