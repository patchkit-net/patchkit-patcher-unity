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
                    if (!www.responseHeaders.ContainsKey("STATUS"))
                    {
                        // Based on tests, if response doesn't contain status it has probably timed out.
                        DebugLogger.Log(string.Format("Response is missing STATUS header. Marking it as timed out."));
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
                            DebugLogger.Log(string.Format("Response has invalid status - {0}. Marking it as timed out.", status));
                            result.WasTimeout = true;
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