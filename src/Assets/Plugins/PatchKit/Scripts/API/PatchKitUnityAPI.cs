using PatchKit.API;
using PatchKit.Unity.API.Web;
using UnityEngine;

namespace PatchKit.Unity.API
{
    public static partial class PatchKitUnity
    {
        private static PatchKitAPI _api;

        public static PatchKitAPI API
        {
            get
            {
                EnsureThatAPIIsCreated();
                return _api;
            }
        }

        public static void EnsureThatAPIIsCreated()
        {
            if (_api == null)
            {
                _api = CreateAPI();
            }
        }

        private static PatchKitAPI CreateAPI()
        {
            var settings = PatchKitUnityAPISettings.GetSettings();

            if (Application.isWebPlayer)
            {
                return new PatchKitAPI(settings, new WebPlayerStringDownloader());
            }
            return new PatchKitAPI(settings);
        }
    }
}