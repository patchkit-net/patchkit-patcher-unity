using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct AppVersion
    {
        /// <summary>
        /// Initial version id.
        /// </summary>
        [JsonProperty("id")]
        public int Id;
        
        /// <summary>
        /// Version label.
        /// </summary>
        [JsonProperty("label")]
        public string Label;
        
        /// <summary>
        /// Description of changes.
        /// </summary>
        [JsonProperty("changelog")]
        public string Changelog;
        
        /// <summary>
        /// Unix timestamp of publish date.
        /// </summary>
        [JsonProperty("publish_date")]
        public long PublishDate;
        
        /// <summary>
        /// Guid of content file.
        /// </summary>
        [JsonProperty("content_guid")]
        public string ContentGuid;
        
        /// <summary>
        /// Guid of content meta file if available.
        /// </summary>
        [JsonProperty("content_meta_guid")]
        public string ContentMetaGuid;
        
        /// <summary>
        /// Guid of diff file or null if there's no diff.
        /// </summary>
        [JsonProperty("diff_guid")]
        public string DiffGuid;
        
        /// <summary>
        /// Guid of diff meta file if available.
        /// </summary>
        [JsonProperty("diff_meta_guid")]
        public string DiffMetaGuid;
        
        /// <summary>
        /// Set to true if this version is a draft version.
        /// </summary>
        [JsonProperty("draft")]
        public bool Draft;
        
        [JsonProperty("pending_publish")]
        public bool PendingPublish;
        
        [JsonProperty("published")]
        public bool Published;
        
        /// <summary>
        /// If pending_publish is true, this number will indicate the publishing progress.
        /// </summary>
        [JsonProperty("publish_progress")]
        public float PublishProgress;
        
        /// <summary>
        /// Main executable relative path without slash at the beginning.
        /// </summary>
        [JsonProperty("main_executable")]
        public string MainExecutable;
        
        /// <summary>
        /// Main executable arguments
        /// </summary>
        [JsonProperty("main_executable_args")]
        public string MainExecutableArgs;
        
        /// <summary>
        /// Relative list of paths that should be ignored for local data consistency.
        /// </summary>
        [JsonProperty("ignored_files")]
        public string[] IgnoredFiles;
        
        /// <summary>
        /// Set to true if version will be published after successfull processing.
        /// </summary>
        [JsonProperty("publish_when_processed")]
        public bool PublishWhenProcessed;
        
        [JsonProperty("processing_started_at")]
        public string ProcessingStartedAt;
        
        [JsonProperty("processing_finished_at")]
        public string ProcessingFinishedAt;
        
        /// <summary>
        /// If true then this version can be imported to other application. Visible only for owners.
        /// </summary>
        [JsonProperty("can_be_imported")]
        public bool CanBeImported;
        
    }
}
