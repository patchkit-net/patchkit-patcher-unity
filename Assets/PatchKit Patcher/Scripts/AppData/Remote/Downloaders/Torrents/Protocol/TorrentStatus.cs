using Newtonsoft.Json;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents.Protocol
{
    public class TorrentStatus
    {
        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("is_seeding")]
        public bool IsSeeding { get; set; }

        [JsonProperty("last_scrape")]
        public long LastScrape { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("paused")]
        public bool Paused { get; set; }

        [JsonProperty("peers")]
        public long Peers { get; set; }

        [JsonProperty("progress")]
        public double Progress { get; set; }

        [JsonProperty("seed_rank")]
        public long SeedRank { get; set; }

        [JsonProperty("seeding_time")]
        public long SeedingTime { get; set; }

        [JsonProperty("seeds")]
        public long Seeds { get; set; }

        [JsonProperty("total_upload")]
        public long TotalUpload { get; set; }

        [JsonProperty("total_wanted")]
        public long TotalWanted { get; set; }
    }
}