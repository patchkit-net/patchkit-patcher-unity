using System;

namespace PatchKit.Api
{
    /// <summary>
    /// Occurs when there are problems with connection to one API server.
    /// </summary>
    /// <seealso cref="System.Exception"/>
    public class ApiServerConnectionException : Exception
    {
        /// <inheritdoc />
        public ApiServerConnectionException(string reason) : base(reason)
        {
        }
    }
}