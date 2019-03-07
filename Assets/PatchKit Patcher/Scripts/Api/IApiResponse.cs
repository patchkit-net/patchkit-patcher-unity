using System;
using Newtonsoft.Json.Linq;
using PatchKit.Network;

namespace PatchKit.Api
{
    /// <summary>
    /// API response.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IApiResponse : IDisposable
    {
        /// <summary>
        /// HTTP web response.
        /// </summary>
        IHttpResponse HttpResponse { get; }

        /// <summary>
        /// Response body.
        /// </summary>
        string Body { get; }

        /// <summary>
        /// Returns body parsed to JSON.
        /// </summary>
        JToken GetJson();
    }
}
