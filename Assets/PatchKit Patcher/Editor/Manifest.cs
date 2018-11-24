using Newtonsoft.Json;

namespace PatchKit.Unity.Editor
{
    public struct Manifest
    {
        public struct Argument
        {
            [JsonProperty(PropertyName = "value")]
            public string[] Value;
        }

        [JsonProperty(PropertyName = "exe_fileName")]
        public string ExeFileName;

        [JsonProperty(PropertyName = "exe_arguments")]
        public string ExeArguments;

        [JsonProperty(PropertyName = "manifest_version")]
        public int Version;

        [JsonProperty(PropertyName = "target")]
        public string Target;

        [JsonProperty(PropertyName = "target_arguments")]
        public Argument[] Arguments;

        [JsonProperty(PropertyName = "capabilities")]
        public string[] Capabilities;
    }
}