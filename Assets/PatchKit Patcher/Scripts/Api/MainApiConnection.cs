namespace PatchKit.Api
{
    /// <summary>
    /// PatchKit Main Api Connection.
    /// </summary>
    public sealed partial class MainApiConnection : ApiConnection
    {
        /// <summary>
        /// Returns default settings.
        /// </summary>
        public static ApiConnectionSettings GetDefaultSettings()
        {
            return new ApiConnectionSettings
            {
                MainServer = new ApiConnectionServer
                {
                    Host = "api2.patchkit.net",
                    UseHttps = true
                },
                CacheServers =
                    new[]
                    {
                        new ApiConnectionServer
                        {
                            Host = "api-cache.patchkit.net",
                            UseHttps = true
                        }
                    }
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainApiConnection"/> class.
        /// </summary>
        /// <param name="connectionSettings">The connection settings.</param>
        public MainApiConnection(ApiConnectionSettings connectionSettings) : base(connectionSettings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainApiConnection"/> class.
        /// </summary>
        public MainApiConnection() : this(GetDefaultSettings())
        {
        }
    }
}