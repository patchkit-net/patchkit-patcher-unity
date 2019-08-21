using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using PatchKit.Api;
using PatchKit.Api.Models.Main;
using PatchKit.Logging;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    class ExternalAuth
    {
        private static ILogger Logger {
            get {
                return Debug.PatcherLogManager.DefaultLogger;
            }
        }

        // how long allow the endpoint to fail
        private static readonly int GracePeriodSeconds = 30;

        private static readonly int LoginRequestIdLength = 32;
        

        public static EndpointResponse TryRefreshToken(
            string refreshToken,
            ExternalAuthConfig config,
            CancellationToken cancellationToken)
        {
            if (!config.IncludeRefreshToken || string.IsNullOrEmpty(refreshToken))
            {
                // refresh token not supported by config
                return null;
            }

            try
            {
                return QueryRefreshTokenEndpoint(refreshToken, config, cancellationToken);
            } catch (WebException e)
            {
                UnityEngine.Debug.LogWarning("Error while talking to the refresh endpoint, ignoring...");
                UnityEngine.Debug.LogException(e);
            } catch (SocketException e)
            {
                UnityEngine.Debug.LogWarning("Error while talking to the refresh endpoint, ignoring...");
                UnityEngine.Debug.LogException(e);
            }

            return null;
        }

        // Opens the login page
        // Returns loginRequestId string to use in `WaitForEndpoint()`
        public static string OpenLoginPage(ExternalAuthConfig config)
        {
            string loginRequestId = GenerateLoginRequestHash();
            OpenURL(config.LoginPageUrl + "?" + config.RequestIdParam + "=" + loginRequestId);
            return loginRequestId;
        }

        // Waits on endpoint to return a valid response.
        // Returns null of failed authentication.
        public static EndpointResponse WaitForEndpoint(
            string loginRequestId,
            ExternalAuthConfig config,
            CancellationToken cancellationToken)
        {
            UnityEngine.Debug.Log("ExternalAuth: Waiting for endpoint.");
            
            long startTime = CurrentTimeSeconds();
            while (startTime + config.EndpointQueryTimeoutSeconds > CurrentTimeSeconds())
            {
                System.Threading.Thread.Sleep(config.EndpointQueryIntervalSeconds * 1000);

                try
                {
                    bool allowToFail = CurrentTimeSeconds() - startTime <= GracePeriodSeconds;
                    EndpointResponse response =
                        QueryEndpoint(loginRequestId, allowToFail, config, cancellationToken);
                    
                    switch(response.EndpointResponseStatus) {
                        case EndpointResponseStatus.Continue:
                            return response;
                        case EndpointResponseStatus.Terminate:
                            return null;
                        case EndpointResponseStatus.Wait:
                            break; // wait for another iteration
                        default:
                            Debug.Assert.IsTrue(false, "Unknown response: " + response);
                            return null;
                    }
                } catch (WebException e)
                {
                    UnityEngine.Debug.LogWarning("Error while talking to the endpoint, ignoring...");
                    UnityEngine.Debug.LogException(e);
                } catch (SocketException e)
                {
                    UnityEngine.Debug.LogWarning("Error while talking to the endpoint, ignoring...");
                    UnityEngine.Debug.LogException(e);
                }
            }

            UnityEngine.Debug.LogWarning("ExternalAuth: Timeout while waiting for endpoint");
            return null;
        }

        private static void OpenURL(string url)
        {
#if UNITY_STANDALONE
            UnityEngine.Application.OpenURL(url);
#else
#error Not implemented
#endif
        }

        private static long CurrentTimeSeconds()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
        }

        private static EndpointResponse QueryRefreshTokenEndpoint(
            string refreshToken,
            ExternalAuthConfig config,
            CancellationToken cancellationToken
        ) {
            IHttpResponse httpResponse =
                PostURL(config.EndpointUrl,
                    new Dictionary<string, string> {
                        { config.RefreshTokenParam, refreshToken }
                    },
                    cancellationToken);

            if ((int) httpResponse.StatusCode == 200)
            {
                StreamReader reader = new StreamReader(httpResponse.ContentStream);
                string httpBody = reader.ReadToEnd();
                JObject obj = JObject.Parse(httpBody);

                EndpointResponse response = CreateOKEndpointResponse(config, obj);
                return response;
            }

            return null;
        }

        private static EndpointResponse QueryEndpoint(
            string loginRequestId,
            bool ignoreNotRecognized,
            ExternalAuthConfig config,
            CancellationToken cancellationToken
        ) {
            IHttpResponse httpResponse =
                FetchURL(config.EndpointUrl, config.RequestIdParam + "=" + loginRequestId, cancellationToken);

            StreamReader reader = new StreamReader(httpResponse.ContentStream);
            string httpBody = reader.ReadToEnd();
            
            int statusCode = (int) httpResponse.StatusCode;
            UnityEngine.Debug.Log(statusCode.ToString());

            if (statusCode == config.EndpointOkResponse)
            {
                UnityEngine.Debug.Log(httpBody);
                JObject obj = JObject.Parse(httpBody);

                EndpointResponse response = CreateOKEndpointResponse(config, obj);
                return response;
            }
            else if (statusCode == config.EndpointWaitResponse)
            {
                return new EndpointResponse(EndpointResponseStatus.Wait);
            } else if (statusCode == config.EndpointForbiddenResponse)
            {
                return new EndpointResponse(EndpointResponseStatus.Terminate);
            } else if (statusCode == config.EndpointNotRecognizedResponse)
            {
                // We may need to wait a while for the endpoint to recognize our login attempt
                if (ignoreNotRecognized) {
                    return new EndpointResponse(EndpointResponseStatus.Wait);
                } else {
                    return new EndpointResponse(EndpointResponseStatus.Terminate);
                }
            } else {
                UnityEngine.Debug.LogError("ExternalAuth: Unknown response from the endpoint: " +
                    httpResponse.StatusCode + ", " + httpBody);
                return new EndpointResponse(EndpointResponseStatus.Wait);
            }
        }

        private static EndpointResponse CreateOKEndpointResponse(ExternalAuthConfig config, JObject obj)
        {
            string refreshToken = null;

            if (config.IncludeRefreshToken)
            {
                refreshToken = (string)obj.SelectToken(config.RefreshTokenPath);
            }

            var response = new EndpointResponse(EndpointResponseStatus.Continue, refreshToken);

            JToken executionArgs = obj.SelectToken(config.ExecutionArgsPath);
            foreach (JToken item in executionArgs)
            {
                var key = (string)item.SelectToken("key");
                var value = (string)item.SelectToken("value");

                response.ExecutionArgs.Add(key, value);
            }

            return response;
        }

        private static IHttpResponse FetchURL(string url, string query, CancellationToken cancellationToken)
        {
            var uri = new Uri(url);
            
            var uriWithQuery = new UriBuilder
            {
                Scheme = uri.Scheme,
                Host = uri.Host,
                Path = uri.AbsolutePath,
                Query = query,
                Port = uri.Port
            }.Uri;

            // UnityEngine.Debug.Log(uriWithQuery.ToString());
            // Logger.LogDebug(uriWithQuery.ToString());

            var httpRequest = new HttpGetRequest
            {
                Address = uriWithQuery,
                Timeout = 60
            };

            var httpClient = new DefaultHttpClient();
            return httpClient.Get(httpRequest);
        }

        private static IHttpResponse PostURL(string url, Dictionary<string, string> values, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            foreach (var pair in values)
            {
                if (sb.Length > 0) {
                    sb.Append("&");
                }
                sb.Append(Uri.EscapeUriString(pair.Key));
                sb.Append('=');
                sb.Append(Uri.EscapeUriString(pair.Value));
            }
            
            var httpRequest = new HttpPostRequest
            {
                Address = new Uri(url),
                Timeout = 60,
                Body = sb.ToString()
            };
            var httpClient = new DefaultHttpClient();
            UnityEngine.Debug.Log("a");
            return httpClient.Post(httpRequest);
        }

        private static string GenerateLoginRequestHash()
        {
            var random = new Random();
            
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, LoginRequestIdLength)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public enum EndpointResponseStatus {
            Wait,
            Continue,
            Terminate
        }

        public class EndpointResponse
        {
            public EndpointResponseStatus EndpointResponseStatus { get; set; }

            public string RefreshToken { get; set; }

            public Dictionary<string, string> ExecutionArgs { get; set; }

            public EndpointResponse(EndpointResponseStatus status, string refreshToken = null)
            {
                EndpointResponseStatus = status;
                RefreshToken = refreshToken;
                ExecutionArgs = new Dictionary<string, string>();
            }
        }
    }
}