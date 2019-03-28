using System;
using JetBrains.Annotations;

namespace PatchKit.Api
{
    /// <summary>
    /// Describes API server.
    /// </summary>
    [Serializable]
    public struct ApiConnectionServer
    {
        /// <summary>
        /// Server host url.
        /// </summary>
        [NotNull]
        public string Host;

        /// <summary>
        /// Port used for connection with server.
        /// </summary>
        public int Port;

        /// <summary>
        /// Actual port used for connection with server.
        /// If <see cref="Port"/> is set to <c>0</c>, other values are used:
        /// - when <see cref="UseHttps"/> is set to <c>false</c> then used port is <c>80</c>
        /// - when <see cref="UseHttps"/> is set to <c>true</c> then used port is <c>443</c>
        /// Otherwise, returns value provided by <see cref="Port"/>.
        /// </summary>
        internal int RealPort
        {
            get
            {
                if (Port == 0)
                {
                    return UseHttps ? 443 : 80;
                }
                return Port;
            }
        }

        /// <summary>
        /// Set to true to use https instead of http.
        /// </summary>
        public bool UseHttps;
    }
}
