using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using PatchKit.Logging;
using PatchKit.Network;

namespace PatchKit.Api
{
    /// <summary>
    /// Base Api Connection.
    /// </summary>
    public class ApiConnection
    {
        private struct Request
        {
            public string Path;

            public string Query;

            public List<Exception> MainServerExceptions;

            public List<Exception> CacheServersExceptions;
        }

        private readonly ApiConnectionSettings _connectionSettings;

        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public IRequestTimeoutCalculator RequestTimeoutCalculator = new SimpleRequestTimeoutCalculator();

        public IRequestRetryStrategy RequestRetryStrategy = new NoneRequestRetryStrategy();

        public IHttpClient HttpClient = new DefaultHttpClient();

        public ILogger Logger = DummyLogger.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiConnection"/> class.
        /// </summary>
        /// <param name="connectionSettings">The connection settings.</param>
        /// <exception cref="System.ArgumentNullException">
        /// connectionSettings - <see cref="ApiConnectionServer.Host"/> of one of the servers is null.
        /// or
        /// connectionSettings - <see cref="ApiConnectionServer.Host"/> of one of the servers is empty.
        /// </exception>
        public ApiConnection(ApiConnectionSettings connectionSettings)
        {
            ThrowIfServerIsInvalid(connectionSettings.MainServer);
            if (connectionSettings.CacheServers != null)
            {
                foreach (var cacheServer in connectionSettings.CacheServers)
                {
                    ThrowIfServerIsInvalid(cacheServer);
                }
            }

            _connectionSettings = connectionSettings;
            _jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
        }

        // ReSharper disable once UnusedParameter.Local
        private static void ThrowIfServerIsInvalid(ApiConnectionServer server)
        {
            if (server.Host == null)
            {
                // ReSharper disable once NotResolvedInText
                throw new ArgumentNullException("connectionSettings",
                    "ApiConnectionServer.Host of one of the servers is null.");
            }

            if (string.IsNullOrEmpty(server.Host))
            {
                // ReSharper disable once NotResolvedInText
                throw new ArgumentNullException("connectionSettings",
                    "ApiConnectionServer.Host of one of the servers is empty");
            }
        }

        /// <summary>
        /// Parses the response data to structure.
        /// </summary>
        protected T ParseResponse<T>(IApiResponse response)
        {
            return JsonConvert.DeserializeObject<T>(response.Body, _jsonSerializerSettings);
        }

        private bool TryGetResponse(ApiConnectionServer server, Request request, ServerType serverType,
            out IApiResponse response)
        {
            Logger.LogDebug(
                $"Trying to get response from server ({serverType}): '{server.Host}:{server.RealPort}' (uses HTTPS: {server.UseHttps})...");

            response = null;

            List<Exception> exceptionsList;

            switch (serverType)
            {
                case ServerType.MainServer:
                    exceptionsList = request.MainServerExceptions;
                    break;
                case ServerType.CacheServer:
                    exceptionsList = request.CacheServersExceptions;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serverType), serverType, null);
            }

            try
            {
                var uri = new UriBuilder
                {
                    Scheme = server.UseHttps ? "https" : "http",
                    Host = server.Host,
                    Path = request.Path,
                    Query = request.Query,
                    Port = server.RealPort
                }.Uri;

                var httpRequest = new HttpGetRequest
                {
                    Address = uri,
                    Timeout = RequestTimeoutCalculator.Timeout
                };

                Logger.LogTrace($"timeout = {httpRequest.Timeout}ms");

                var httpResponse = HttpClient.Get(httpRequest);

                Logger.LogDebug("Received response. Checking whether it is valid...");
                Logger.LogTrace($"Response status code: {httpResponse.StatusCode}");

                if (IsResponseValid(httpResponse, serverType))
                {
                    Logger.LogDebug("Response is valid.");
                    response = new ApiResponse(httpResponse);
                    return true;
                }

                Logger.LogWarning("Response is not valid.");

                if (IsResponseUnexpectedError(httpResponse, serverType))
                {
                    throw new ApiResponseException((int) httpResponse.StatusCode);
                }

                throw new ApiServerConnectionException(
                    $"Server \'{server.Host}\' returned code {(int) httpResponse.StatusCode}");
            }
            catch (WebException webException)
            {
                Logger.LogWarning("Error while connecting to the API server.", webException);
                exceptionsList.Add(webException);
                return false;
            }
            catch (ApiServerConnectionException e)
            {
                Logger.LogWarning("Error while connecting to the API server.", e);
                exceptionsList.Add(e);
                return false;
            }
        }

        private bool IsResponseValid(IHttpResponse httpResponse, ServerType serverType)
        {
            switch (serverType)
            {
                case ServerType.MainServer:
                    return IsStatusCodeOk(httpResponse.StatusCode);
                case ServerType.CacheServer:
                    return httpResponse.StatusCode == HttpStatusCode.OK;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serverType), serverType, null);
            }
        }

        private bool IsResponseUnexpectedError(IHttpResponse httpResponse, ServerType serverType)
        {
            switch (serverType)
            {
                case ServerType.MainServer:
                    return !IsStatusCodeOk(httpResponse.StatusCode) &&
                           !IsStatusCodeServerError(httpResponse.StatusCode);
                case ServerType.CacheServer:
                    return false; // ignore any api cache error
                default:
                    throw new ArgumentOutOfRangeException(nameof(serverType), serverType, null);
            }
        }

        private bool IsStatusCodeOk(HttpStatusCode statusCode)
        {
            return IsWithin((int) statusCode, 200, 299);
        }

        private bool IsStatusCodeServerError(HttpStatusCode statusCode)
        {
            return IsWithin((int) statusCode, 500, 599);
        }

        private bool IsWithin(int value, int min, int max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Retrieves specified resource from API.
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <param name="query">The query of the resource.</param>
        /// <returns>Response with resource result.</returns>
        /// <exception cref="ApiConnectionException">Could not connect to API.</exception>
        public IApiResponse GetResponse(string path, string query)
        {
            try
            {
                Logger.LogDebug($"Getting response for path: '{path}' and query: '{query}'...");

                var request = new Request
                {
                    Path = path,
                    Query = query,
                    MainServerExceptions = new List<Exception>(),
                    CacheServersExceptions = new List<Exception>()
                };

                IApiResponse apiResponse;

                bool retry;

                do
                {
                    if (!TryGetResponse(_connectionSettings.MainServer, request, ServerType.MainServer,
                        out apiResponse))
                    {
                        if (_connectionSettings.CacheServers != null)
                        {
                            foreach (var cacheServer in _connectionSettings.CacheServers)
                            {
                                if (TryGetResponse(cacheServer, request, ServerType.CacheServer, out apiResponse))
                                {
                                    break;
                                }
                            }
                        }
                    }

                    if (apiResponse == null)
                    {
                        Logger.LogWarning(
                            "Connection attempt to every server has failed. Checking whether retry is possible...");
                        RequestTimeoutCalculator.OnRequestFailure();
                        RequestRetryStrategy.OnRequestFailure();

                        retry = RequestRetryStrategy.ShouldRetry;

                        if (!retry)
                        {
                            Logger.LogError("Retry is not possible.");
                            throw new ApiConnectionException(request.MainServerExceptions,
                                request.CacheServersExceptions);
                        }

                        Logger.LogDebug(
                            $"Retry is possible. Waiting {RequestRetryStrategy.DelayBeforeNextTry}ms before next attempt...");

                        Thread.Sleep(RequestRetryStrategy.DelayBeforeNextTry);

                        Logger.LogDebug("Trying to get response from servers once again...");
                    }
                    else
                    {
                        retry = false;
                    }
                } while (retry);

                Logger.LogDebug("Successfully got response.");
                Logger.LogTrace($"Response body: {apiResponse.Body}");

                RequestTimeoutCalculator.OnRequestSuccess();
                RequestRetryStrategy.OnRequestSuccess();

                return apiResponse;
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to get response.", e);
                throw;
            }
        }

        private enum ServerType
        {
            MainServer,
            CacheServer
        }
    }
}