using System;

namespace PatchKit.Api
{
    /// <summary>
    /// Occurs when there are problems with API response.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class ApiResponseException : Exception
    {
        /// <summary>
        /// API status code.
        /// </summary>
        public int StatusCode { get; }

        /// <inheritdoc />
        public ApiResponseException(int statusCode) : base($"API server returned invalid status code {statusCode}")
        {
            StatusCode = statusCode;
        }
    }
}