using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct App
    {
        /// <summary>
        /// Initial app id.
        /// </summary>
        [JsonProperty("id")]
        public int Id;
        
        /// <summary>
        /// Application secret
        /// </summary>
        [JsonProperty("secret")]
        public string Secret;
        
        /// <summary>
        /// Application platfrom
        /// </summary>
        [JsonProperty("platform")]
        public string Platform;
        
        /// <summary>
        /// Application name
        /// </summary>
        [JsonProperty("name")]
        public string Name;
        
        /// <summary>
        /// Application display name.
        /// </summary>
        [JsonProperty("display_name")]
        public string DisplayName;
        
        /// <summary>
        /// Application author.
        /// </summary>
        [JsonProperty("author")]
        public string Author;
        
        /// <summary>
        /// If set to true, application needs to contact keys server to get valid key_secret for content download.
        /// </summary>
        [JsonProperty("use_keys")]
        public bool UseKeys;
        
        [JsonProperty("publish_method")]
        public string PublishMethod;
        
        /// <summary>
        /// Current number of publicly available version (does not count drafts). If 0, no version has been yet published.
        /// </summary>
        [JsonProperty("current_version")]
        public int CurrentVersion;
        
        /// <summary>
        /// The number of lowest version that has a diff file available. For instance if player has version 3 and lowest_version_with_diff=4 then the player can upgrade from version 3 to 4 using diff file. But when lowest_version_with_diff=5 then it's not possible to apply a diff file and player has to re-download the game instead.
        /// </summary>
        [JsonProperty("lowest_version_with_diff")]
        public int LowestVersionWithDiff;
        
        [JsonProperty("lowest_version_with_content")]
        public int LowestVersionWithContent;
        
        /// <summary>
        /// An https url to image banner that should be displayed inside the patcher. Watch out for patcher_banner_image_dimensions, but for now the size will be fixed at 600x230. If this field is set to null, a default (stored) image should be used. The image will be always in PNG format.
        /// </summary>
        [JsonProperty("patcher_banner_image")]
        public string PatcherBannerImage;
        
        [JsonProperty("patcher_banner_image_dimensions")]
        public PatcherBannerImageDimensions PatcherBannerImageDimensions;
        
        /// <summary>
        /// Date and time when patcher banner image has been updated.
        /// </summary>
        [JsonProperty("patcher_banner_image_updated_at")]
        public string PatcherBannerImageUpdatedAt;
        
        /// <summary>
        /// Tells the patcher what format should be used to display download speed unit. human_readable should display kilobytes unless download speed exceeds 1024 kilobytes/s, then megabytes should be displayed.
        /// </summary>
        [JsonProperty("patcher_download_speed_unit")]
        public string PatcherDownloadSpeedUnit;
        
        /// <summary>
        /// If set to true, no PatchKit logo or PatchKit reference should be visible on the patcher.
        /// </summary>
        [JsonProperty("patcher_whitelabel")]
        public bool PatcherWhitelabel;
        
        /// <summary>
        /// The secret of patcher to use.
        /// </summary>
        [JsonProperty("patcher_secret")]
        public string PatcherSecret;
        
        /// <summary>
        /// Visible only for the application owner. Set to true if this application is a channel.
        /// </summary>
        [JsonProperty("is_channel")]
        public bool IsChannel;
        
        [JsonProperty("parent_group")]
        public AppRef ParentGroup;
        
        /// <summary>
        /// Visible only for the application owner. If is_channel is set to false, this field contains a list of children channels.
        /// </summary>
        [JsonProperty("children_channels")]
        public AppRef[] ChildrenChannels;
        
    }
}
