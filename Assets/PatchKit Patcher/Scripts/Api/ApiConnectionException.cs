using System;
using System.Collections.Generic;

namespace PatchKit.Api
{
    /// <summary>
    /// Occurs when there are problems with connection to API.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class ApiConnectionException : Exception
    {
        /// <inheritdoc />
        public ApiConnectionException(IEnumerable<Exception> mainServerExceptions,
            IEnumerable<Exception> cacheServersExceptions) : base("Unable to connect to any of the API servers.")
        {
            MainServerExceptions = mainServerExceptions;
            CacheServersExceptions = cacheServersExceptions;
        }

        /// <summary>
        /// Exceptions that occured during attempts to connect to main server.
        /// </summary>
        public IEnumerable<Exception> MainServerExceptions { get; }

        /// <summary>
        /// Exceptions that occured during attempts to connect to cache servers.
        /// </summary>
        public IEnumerable<Exception> CacheServersExceptions { get; }

        /// <inheritdoc />
        public override string Message
        {
            get
            {
                var t = base.Message;

                t += "\n" +
                     "Main server exceptions:\n" +
                     ExceptionsToString(MainServerExceptions) +
                     "Cache servers exceptions:\n" +
                     ExceptionsToString(CacheServersExceptions);

                return t;
            }
        }

        private static string ExceptionsToString(IEnumerable<Exception> exceptions)
        {
            var result = string.Empty;

            int i = 1;
            foreach (var t in exceptions)
            {
                result += $"{i}. {t}\n";
                i++;
            }

            if (i == 1)
            {
                result = "(none)";
            }

            return result;
        }
    }
}