using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct Changelog
    {
        /// <summary>
        /// Structure Versions.
        /// </summary>
        [JsonProperty("versions")]
        public Versions[] versions;
    }
}
