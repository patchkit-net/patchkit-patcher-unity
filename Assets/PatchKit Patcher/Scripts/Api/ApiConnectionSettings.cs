using System;
using JetBrains.Annotations;

namespace PatchKit.Api
{
    /// <summary>
    /// <see cref="ApiConnection" /> settings.
    /// </summary>
    [Serializable]
    public struct ApiConnectionSettings
    {
        /// <summary>
        /// Main API server.
        /// </summary>
        public ApiConnectionServer MainServer;

        /// <summary>
        /// Cache API servers. Priority of servers is based on the array order.
        /// </summary>
        [CanBeNull] public ApiConnectionServer[] CacheServers;
    }
}