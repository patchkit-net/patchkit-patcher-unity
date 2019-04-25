using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using PatchKit.Logging;
using UnityEngine;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
using ILogger = PatchKit.Logging.ILogger;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class UnityHttpClient : IHttpClient
    {
        private class WWWResult
        {
            public bool IsDone { get; set; }

            public string Text { get; set; }

            public Dictionary<string, string> ResponseHeaders { get; set; }
        }

        private readonly ILogger _logger;

        public UnityHttpClient()
        {
            _logger = PatcherLogManager.DefaultLogger;
        }

        private IEnumerator GetWWW(HttpGetRequest getRequest, WWWResult result)
        {
            var www = new WWW(getRequest.Address.ToString());

            yield return www;

            lock (result)
            {
                result.IsDone = www.isDone;

                if (www.isDone)
                {
                    result.ResponseHeaders = www.responseHeaders;
                    result.Text = www.text;
                }
            }
        }

        public IHttpResponse Get(HttpGetRequest getRequest)
        {
            try
            {
                _logger.LogDebug("Sending GET request to " + getRequest.Address);

                if (getRequest.Range != null)
                {
                    throw new NotImplementedException();
                }

                _logger.LogTrace("timeout  = " + getRequest.Timeout);
            
                var result = new WWWResult();

                var waitHandle = UnityDispatcher.InvokeCoroutine(GetWWW(getRequest, result));
            
                waitHandle.WaitOne(TimeSpan.FromMilliseconds(getRequest.Timeout));

                lock (result)
                {
                    if (!result.IsDone)
                    {
                        throw new WebException("Timeout.", WebExceptionStatus.Timeout);
                    }

                    var statusCode = ReadStatusCode(result);

                    _logger.LogDebug("Successfuly received response.");
                    return new UnityHttpResponse(result.Text, statusCode);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to get response.", e);
                throw;
            }
        }

        private HttpStatusCode ReadStatusCode(WWWResult result)
        {
            _logger.LogDebug("Reading status code...");

            if (result.ResponseHeaders == null || !result.ResponseHeaders.ContainsKey("STATUS"))
            {
                // Based on tests, if response doesn't contain status it has probably timed out.
                _logger.LogWarning("Response is missing STATUS header. Marking it as timed out.");
                throw new WebException("Timeout.", WebExceptionStatus.Timeout);
            }

            var status = result.ResponseHeaders["STATUS"];
            _logger.LogTrace("status = " + status);

            var s = status.Split(' ');

            int statusCode;

            if (s.Length < 3 || !int.TryParse(s[1], out statusCode))
            {
                _logger.LogWarning("Response has invalid status. Marking it as timed out.");
                throw new WebException("Timeout.", WebExceptionStatus.Timeout);
            }

            _logger.LogTrace("statusCode = " + statusCode);
            _logger.LogTrace("statusCode (as enum) = " + (HttpStatusCode) statusCode);

            return (HttpStatusCode) statusCode;
        }

        public IHttpResponse Post(HttpPostRequest postRequest)
        {
            throw new NotImplementedException();
        }
    }
}