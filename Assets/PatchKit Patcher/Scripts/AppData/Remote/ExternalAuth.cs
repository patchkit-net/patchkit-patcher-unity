using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using PatchKit.Api.Models.Main;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
class ExternalAuth
{
    public class OngoingAuth
    {
        public string LoginRequestId;
    }

    public class AuthResult
    {
        public string RefreshToken { get; }

        public Dictionary<string, string> ExecutionArgs { get; }

        public AuthResult(
            string refreshToken,
            Dictionary<string, string> executionArgs)
        {
            RefreshToken = refreshToken;
            ExecutionArgs = executionArgs;
        }
    }

    public class EndpointConnectionFailureException : Exception
    {
        public EndpointConnectionFailureException(
            string message, 
            Exception inner = null) : base(message, inner)
        {
        }
    }

    public class EndpointInvalidConfigurationException : Exception
    {
        public EndpointInvalidConfigurationException(
            string message, 
            Exception inner = null) : base(message, inner)
        {
        }
    }

    public class InternalErrorException : Exception
    {
        public InternalErrorException(
            string message, 
            Exception inner = null) : base(message, inner)
        {
        }
    }   

    private static readonly DebugLogger DebugLogger = 
        new DebugLogger(typeof(ExternalAuth));

    private enum EndpointResponseStatus 
    {
        Wait,
        Continue,
        Forbidden,
        NotRecognized
    }

    private class EndpointResponse
    {
        public EndpointResponseStatus Status { get; }

        public string RefreshToken { get; }

        public Dictionary<string, string> ExecutionArgs { get; }

        public EndpointResponse(
            EndpointResponseStatus status, 
            string refreshToken,
            Dictionary<string, string> executionArgs)
        {
            Status = status;
            RefreshToken = refreshToken;
            ExecutionArgs = executionArgs;
        }
    }

    private static readonly int LoginRequestIdLength = 32;

    private static readonly int EndpointRecognitionTimoutSeconds = 30;
    
    public static AuthResult TryRefreshToken(
        string refreshToken,
        ExternalAuthConfig config,
        CancellationToken cancellationToken)
    {
        DebugLogger.Log("Trying to refresh token for external auth with id" + config.Id);
        DebugLogger.Log("Refresh token: " + refreshToken);

        try
        {
            bool isRefreshingTokenSupported = config.IncludeRefreshToken;

            if (!isRefreshingTokenSupported)
            {
                DebugLogger.Log("Failed to refresh token -> refreshing token is not supported for " + config.Id);

                return null;
            }

            bool canTokenBeRefreshed = !string.IsNullOrEmpty(refreshToken);

            if (!canTokenBeRefreshed)
            {
                DebugLogger.Log("Failed to refresh token -> cannot refresh empty or null token.");

                return null;
            }

            var response =  QueryRefreshTokenEndpoint(
                refreshToken, 
                config, 
                cancellationToken);
            
            if (response == null)
            {
                DebugLogger.Log("Failed to refresh token -> response is null.");
                return null;
            }

            DebugLogger.Log("Successfully refreshed token.");

            return new AuthResult(
                refreshToken: response.RefreshToken,
                executionArgs: response.ExecutionArgs);
        }
        catch (EndpointConnectionFailureException e)
        {
            DebugLogger.Log("Failed to refresh token -> endpoint connection failure.");
            DebugLogger.LogException(e);
            return null;
        }
        catch (EndpointInvalidConfigurationException e)
        {
            DebugLogger.Log("Failed to refresh token -> endpoint invalid configuration.");
            DebugLogger.LogException(e);
            return null;
        }
        catch (InternalErrorException e)
        {
            DebugLogger.Log("Failed to refresh token -> internal error.");
            DebugLogger.LogException(e);
            return null;
        }
        catch (Exception e)
        {
            DebugLogger.Log("Failed to refresh token -> unpredicted exception.");
            DebugLogger.LogException(e);
            return null;
        }
    }

    public static OngoingAuth BeginAuth(ExternalAuthConfig config)
    {
        DebugLogger.Log("Beginning auth for external auth with id " + config.Id);

        try
        {
            string loginRequestId = GenerateNewLoginRequestId();

            string urlArgs = config.RequestIdParam + "=" + loginRequestId;
            string url = config.LoginPageUrl + "?" + urlArgs;

            DebugLogger.Log("Opening auth page at " + url);

            OpenUrl(url);

            DebugLogger.Log("Auth began successfully with login request id: " + loginRequestId);

            return new OngoingAuth
            {
                LoginRequestId = loginRequestId
            };
        }
        catch (InternalErrorException e)
        {
            DebugLogger.Log("Failed to begin auth -> internal error.");
            DebugLogger.LogException(e);
            return null;
        }
        catch (Exception e)
        {
            DebugLogger.Log("Failed to begin auth -> unpredicted exception.");
            DebugLogger.LogException(e);
            return null;
        }
    }

    public static AuthResult WaitForAuth(
        OngoingAuth ongoingAuth,
        ExternalAuthConfig config,
        CancellationToken cancellationToken)
    {
        DebugLogger.Log("Waiting for auth for " + ongoingAuth.LoginRequestId);

        try
        {
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds < 
                config.EndpointQueryTimeoutSeconds)
            {
                System.Threading.Thread.Sleep(
                    config.EndpointQueryIntervalSeconds * 1000);

                try
                {
                    bool hasRecognitionTimedOut = 
                        (DateTime.Now - startTime).TotalSeconds <= 
                            EndpointRecognitionTimoutSeconds;
                    
                    EndpointResponse response = QueryEndpoint(
                        ongoingAuth.LoginRequestId, 
                        config,
                        cancellationToken);
                    
                    switch(response.Status) 
                    {
                        case EndpointResponseStatus.Continue:
                            DebugLogger.Log("Successfuly waited for auth.");

                            return new AuthResult(
                                refreshToken: response.RefreshToken,
                                executionArgs: response.ExecutionArgs);
                        case EndpointResponseStatus.Forbidden:
                            DebugLogger.Log("Failed to wait for auth -> endpoint returned failed response.");

                            return null;
                        case EndpointResponseStatus.Wait:
                            DebugLogger.Log("Still waiting for auth -> endpoint returned wait response.");

                            break;
                        case EndpointResponseStatus.NotRecognized:
                            if (hasRecognitionTimedOut)
                            {
                                DebugLogger.Log("Failed to wait for auth -> recognition has timed out.");
                                return null;
                            }
                            
                            DebugLogger.Log("Still waiting for auth -> auth is not recognized yet.");

                            break;                    
                        default:
                            throw new InternalErrorException("Unknown response status: " + response.Status);
                    }
                }
                catch (EndpointConnectionFailureException e)
                {
                    DebugLogger.Log("Still waiting for auth -> failed to connect to endpoint.");
                    DebugLogger.LogException(e);
                }
            }

            DebugLogger.Log("Failed to wait for auth -> waiting has timed out.");
            return null;
        }
        catch (EndpointInvalidConfigurationException e)
        {
            DebugLogger.Log("Failed to wait for auth -> endpoint invalid configuration.");
            DebugLogger.LogException(e);
            return null;
        }
        catch (InternalErrorException e)
        {
            DebugLogger.Log("Failed to wait for auth -> internal error.");
            DebugLogger.LogException(e);
            return null;
        }
        catch (Exception e)
        {
            DebugLogger.Log("Failed to wait for auth -> unpredicted exception.");
            DebugLogger.LogException(e);
            return null;
        }
    }

    private static EndpointResponse QueryRefreshTokenEndpoint(
        string refreshToken,
        ExternalAuthConfig config,
        CancellationToken cancellationToken)
    {
        var httpClient = new DefaultHttpClient();

        var httpAddress = GetRefreshTokenEndpointAddress(
            config: config,
            refreshToken: refreshToken);
        
        DebugLogger.Log("Quering " + httpAddress);
        
        var httpRequest = new HttpGetRequest
        {
            Address = httpAddress,
            Timeout = config.EndpointQueryTimeoutSeconds * 1000
        };

        IHttpResponse httpResponse;

        try
        {
            httpResponse = httpClient.Get(getRequest: httpRequest);
        }
        catch (Exception e)
        {
            DebugLogger.LogException(e);
            throw new EndpointConnectionFailureException(
                "Failed to connect to refresh token endpoint.",
                e);
        }

        using (httpResponse)
        {
            var httpStatusCode = (int) httpResponse.StatusCode;

            if (httpStatusCode == config.RefreshTokenEndpointOkResponse)
            {
                var httpJsonBody = ReadEndpointResponseJsonBody(response: httpResponse);

                return GetEndpointContinueResponse(
                    jsonBody: httpJsonBody,
                    config: config);
            }
        }

        return null;
    }

    private static EndpointResponse QueryEndpoint(
        string loginRequestId,
        ExternalAuthConfig config,
        CancellationToken cancellationToken)
    {
        var httpClient = new DefaultHttpClient();

        var httpAddress = GetEndpointAddress(
            config: config,
            loginRequestId: loginRequestId);

        DebugLogger.Log("Quering " + httpAddress);

        var httpRequest = new HttpGetRequest
        {
            Address = httpAddress,
            Timeout = config.EndpointQueryTimeoutSeconds * 1000
        };

        IHttpResponse httpResponse;

        try
        {
            httpResponse = httpClient.Get(getRequest: httpRequest);
        }
        catch (Exception e)
        {
            DebugLogger.LogException(e);
            throw new EndpointConnectionFailureException(
                "Failed to connect to endpoint.",
                e);
        }

        using (httpResponse)
        {
            var httpStatusCode = (int) httpResponse.StatusCode;

            var httpTextBody = ReadEndpointResponseTextBody(
                response: httpResponse);

            if (httpStatusCode == config.EndpointOkResponse)
            {
                var httpJsonBody = ParseEndpointResponseTextBody(
                    textBody: httpTextBody);

                return GetEndpointContinueResponse(
                    jsonBody: httpJsonBody,
                    config: config);
            }
            else if (httpStatusCode == config.EndpointWaitResponse)
            {
                return new EndpointResponse(
                    status: EndpointResponseStatus.Wait,
                    refreshToken: null,
                    executionArgs: null);
            }
            else if (httpStatusCode == config.EndpointForbiddenResponse)
            {
                return new EndpointResponse(
                    status: EndpointResponseStatus.Forbidden,
                    refreshToken: null,
                    executionArgs: null);
            } 
            else if (httpStatusCode == config.EndpointNotRecognizedResponse)
            {
                return new EndpointResponse(
                    status: EndpointResponseStatus.NotRecognized,
                    refreshToken: null,
                    executionArgs: null);
            } 
            else 
            {
                DebugLogger.LogWarning(
                    "Unknown endpoint response status code (" + httpStatusCode + "), treating it as WaitResponse. Body: " + httpTextBody);
                
                return new EndpointResponse(
                    status: EndpointResponseStatus.Wait,
                    refreshToken: null,
                    executionArgs: null);
            }
        }
    }

    private static EndpointResponse GetEndpointContinueResponse(
        JObject jsonBody,
        ExternalAuthConfig config)
    {
        var newRefreshToken = RetrieveEndpointResponseRefreshToken(
            jsonBody: jsonBody,
            config: config);

        var executionArgs = RetrieveEndpointResponseExecutionArgs(
            jsonBody: jsonBody,
            config: config);
        
        DebugLogger.Log("Refresh Token: " + newRefreshToken);

        return new EndpointResponse(
            status: EndpointResponseStatus.Continue,
            refreshToken: newRefreshToken,
            executionArgs: executionArgs);
    }

    private static string RetrieveEndpointResponseRefreshToken(
        JObject jsonBody,
        ExternalAuthConfig config)
    {
        try
        {
            if (config.IncludeRefreshToken)
            {
                return (string) jsonBody.SelectToken(config.RefreshTokenPath);
            }

            return null;
        }
        catch (Exception e)
        {
            throw new EndpointInvalidConfigurationException(
                "Failed to retrieve endpoint response refresh token.",
                e);
        }
    }

    private static Dictionary<string, string> RetrieveEndpointResponseExecutionArgs(
        JObject jsonBody,
        ExternalAuthConfig config)
    {
        try
        {
            var executionArgs = new Dictionary<string, string>();
            var executionArgsToken = jsonBody.SelectToken(config.ExecutionArgsPath);

            foreach (var item in executionArgsToken)
            {
                var key = (string) item.SelectToken("key");
                var value = (string) item.SelectToken("value");

                executionArgs.Add(key, value);
            }

            return executionArgs;
        }
        catch (Exception e)
        {
            throw new EndpointInvalidConfigurationException(
                "Failed to retrieve endpoint response execution args.",
                e);
        }
    }

    private static Uri GetRefreshTokenEndpointAddress(
        ExternalAuthConfig config,
        string refreshToken)
    {
        string query = "?" + config.RefreshTokenParam + "=" + refreshToken;

        return new Uri(config.RefreshTokenEndpointUrl + query);
    }

    private static Uri GetEndpointAddress(
        ExternalAuthConfig config,
        string loginRequestId)
    {
        string query = "?" + config.RequestIdParam + "=" + loginRequestId;

        return new Uri(config.EndpointUrl + query);
    }

    private static string ReadEndpointResponseTextBody(IHttpResponse response)
    {
        try
        {
            using(var reader = new StreamReader(response.ContentStream))
            {
                return reader.ReadToEnd();
            }
        }
        catch (Exception e)
        {
            throw new EndpointConnectionFailureException(
                "Failed to read endpoint response text body.",
                e);
        }
    }

    private static JObject ParseEndpointResponseTextBody(string textBody)
    {
        try
        {
            return JObject.Parse(json: textBody);
        }
        catch (Exception e)
        {
            throw new EndpointInvalidConfigurationException(
                "Failed to parse endpoint response text body: " + textBody,
                e);
        }
    }

    private static JObject ReadEndpointResponseJsonBody(IHttpResponse response)
    {
        var textBody = ReadEndpointResponseTextBody(response: response);

        return ParseEndpointResponseTextBody(textBody: textBody);
    }

    private static string GenerateNewLoginRequestId()
    {
        return System.Guid.NewGuid().ToString();
    }

    private static void OpenUrl(string url)
    {
#if UNITY_STANDALONE
        PatchKit.Unity.Utilities.UnityDispatcher.Invoke(() =>
        {
            UnityEngine.Application.OpenURL(url);
        }).WaitOne();
#else
#error Not implemented
#endif
    }
}
}