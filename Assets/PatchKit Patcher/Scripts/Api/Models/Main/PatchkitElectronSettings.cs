using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct PatchkitElectronSettings
    {
        [JsonProperty("appPath")]
        public string appPath;
        
    }
}
