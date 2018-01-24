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
            public bool IsDone;

            public string Data;
            public int StatusCode;

            public bool WasError;
            public bool WasTimeout;
            public string ErrorText;
        }
        
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(UnityHttpClient));

        private const string ResponseEncoding = "iso-8859-2";

        private IEnumerator JobCoroutine(HttpGetRequest getRequest, RequestResult result)
        {
            var www = new WWW(getRequest.Address.ToString());

            yield return www;

            if (!www.isDone)
            {
                lock (result)
                {
                    result.WasTimeout = true;
                    result.IsDone = true;
                }
                yield break;
            }

            if (!string.IsNullOrEmpty(www.error))
            {
                lock (result)
                {
                    result.ErrorText = www.error;
                    result.WasError = true;
                    result.IsDone = true;
                }
                yield break;
            }

            var wwwText = www.text;

            if (string.IsNullOrEmpty(wwwText))
            {
                lock (result)
                {
                    result.ErrorText = "Empty response data";
                    result.WasError = true;
                    result.IsDone = true;
                }
                yield break;
            }

            lock (result)
            {
                try
                {
                    result.Data = wwwText;

                    if (!www.responseHeaders.ContainsKey("STATUS"))
                    {
                        // Based on tests, if response doesn't contain status it has probably timed out.
                        DebugLogger.Log("Response is missing STATUS header. Marking it as timed out.");
                        result.WasTimeout = true;
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
                            // Based on tests, if response contains invalid status it has probably timed out.
                            DebugLogger.Log(string.Format("Response has invalid status - {0}. Marking it as timed out.",
                                status));
                            result.WasTimeout = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    result.WasError = true;
                    result.ErrorText = e.ToString();
                }
                finally
                {
                    result.IsDone = true;
                }
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
            
            waitHandle.WaitOne(TimeSpan.FromMilliseconds(getRequest.Timeout));

            if (result.WasTimeout || !result.IsDone)
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