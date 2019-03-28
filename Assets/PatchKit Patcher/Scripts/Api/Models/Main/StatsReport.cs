using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct StatsReport
    {
        [JsonProperty("event_name")]
        public string EventName;
        
        /// <summary>
        /// Secret of game application if applicable.
        /// </summary>
        [JsonProperty("app_secret")]
        public string AppSecret;
        
        /// <summary>
        /// Version of game application if applicable.
        /// </summary>
        [JsonProperty("app_version")]
        public int AppVersion;
        
        /// <summary>
        /// Unique client id. Should remain the same for all applications launched on this machine + account. Used to identify the player.
        /// </summary>
        [JsonProperty("sender_id")]
        public long SenderId;
        
        /// <summary>
        /// Caller id the same as for caller GET parameters. More information: http://redmine.patchkit.net/projects/patchkit-documentation/wiki/Web_API_General_Info
        /// </summary>
        [JsonProperty("caller")]
        public string Caller;
        
        [JsonProperty("operating_system_family")]
        public string OperatingSystemFamily;
        
        /// <summary>
        /// Operating system version without family name, for instance '10.11' for OSX.
        /// </summary>
        [JsonProperty("operating_system_version")]
        public string OperatingSystemVersion;
        
    }
}
