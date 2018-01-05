using System;
using System.Collections;
using System.Net;
using UnityEngine;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class UnityHttpClient : IHttpClient
    {
        private class RequestResult
        {
            public string Data;
            public int StatusCode;

            public bool WasError;
            public bool WasTimeout;
            public string ErrorText;
        }
        
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(UnityHttpClient));

        public const string ResponseEncoding = "iso-8859-2";

        private IEnumerator JobCoroutine(HttpGetRequest getRequest, RequestResult result)
        {
            var www = new WWW(getRequest.Address.ToString());
            var start = DateTime.Now;

            while (!www.isDone && string.IsNullOrEmpty(www.error))
            {
                yield return new WaitForSeconds(0.1f);

                if ((DateTime.Now - start).Milliseconds > getRequest.Timeout)
                {
                    break;
                }
            }

            if (www.isDone || !string.IsNullOrEmpty(www.error))
            {
                result.Data = www.text;

                try
                {
                    // HACK: because WWW is broken and sometimes just does not return STATUS in responseHeaders we are returning status code 200 (we can assume that status code is not an error since www.error is null or empty).
                    if (!www.responseHeaders.ContainsKey("STATUS"))
                    {
                        DebugLogger.LogWarning("Response headers doesn't contain status information. Since WWW marks response as one without errors, status code is set to 200 (OK).");
                        result.StatusCode = 200;
                    }
                    else
                    {
                        var status = www.responseHeaders["STATUS"];
                        DebugLogger.Log(string.Format("Response status: {0}", status));
                        var s = status.Split(' ');

                        if (s.Length >= 3 && int.TryParse(s[1], out result.StatusCode))
                        {
                            DebugLogger.Log(string.Format("Successfully parsed status code: {0}", result.StatusCode));
                        }
                        else
                        {
                            // HACK: Again, we can't parse the status code (it might be in some different format) - so we simply set it to 200.
                            DebugLogger.LogWarning(
                                "Unable to parse status code. Since WWW marks response as one without errors, status code is set to 200 (OK).");
                            result.StatusCode = 200;
                        }
                    }

                }
                catch (Exception e)
                {
                    result.WasError = true;
                    result.ErrorText = e.ToString();
                }
            }
            else
            {
                result.WasTimeout = true;
            }

            yield return null;
        }

        public IHttpResponse Get(HttpGetRequest getRequest)
        {
            if (getRequest.Range != null)
            {
                throw new NotImplementedException();
            }
            
            var result = new RequestResult();
            
            var waitHandle = Utilities.UnityDispatcher.InvokeCoroutine(JobCoroutine(getRequest, result));
            
            waitHandle.WaitOne();

            if (result.WasTimeout)
            {
                throw new WebException("Timeout.", WebExceptionStatus.Timeout);
            }

            if (result.WasError)
            {
                throw new WebException(result.ErrorText);
            }

            return new UnityHttpResponse(result.Data, result.StatusCode, ResponseEncoding);
        }
    }
}