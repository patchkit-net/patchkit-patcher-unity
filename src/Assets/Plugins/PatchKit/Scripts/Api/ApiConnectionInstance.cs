using PatchKit.Api;

namespace PatchKit.Unity.Api
{
    /// <summary>
    /// Instance of <see cref="ApiConnection"/> which can be accessed globally.
    /// Settings of instance are located in Plugins/PatchKit/Resources/PatchKit API Settings.asset
    /// </summary>
    public static class ApiConnectionInstance
    {
        private static ApiConnection _apiConnection;

        public static ApiConnection Instance
        {
            get
            {
                EnsureThatApiIsCreated();
                return _apiConnection;
            }
        }

        public static void EnsureThatApiIsCreated()
        {
            if (_apiConnection == null)
            {
                CreateAPI();
            }
        }

        private static void CreateAPI()
        {
            var connectionSettings = ApiConnectionInstanceSettings.GetConnectionSettings();

            _apiConnection = new ApiConnection(connectionSettings, new ApiHttpDownloader());
        }
    }
}