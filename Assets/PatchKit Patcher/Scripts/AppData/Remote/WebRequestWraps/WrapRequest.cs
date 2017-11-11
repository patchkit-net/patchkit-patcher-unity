using System;
using System.Collections;
using System.Net;
using System.Threading;
using UnityEngine;
using PatchKit.Api;
using PatchKit.Unity.Patcher.Debug;
using ILogger = PatchKit.Logging.ILogger;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class WrapRequest : IHttpWebRequest
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(WrapRequest));

        public const string ResponseEncoding = "iso-8859-2";

        private readonly EventWaitHandle _waitHandle;

        private string _data;
        private int _statusCode;

        private bool _wasError;
        private bool _wasTimeout;
        private string _errorText;

        private IEnumerator JobCoroutine(string url)
        {
            var www = new WWW(url);
            var start = DateTime.Now;

            while (!www.isDone && string.IsNullOrEmpty(www.error))
            {
                yield return new WaitForSeconds(0.1f);

                if ((DateTime.Now - start).Milliseconds > Timeout)
                {
                    break;
                }
            }

            if (www.isDone || !string.IsNullOrEmpty(www.error))
            {
                _data = www.text;

                try
                {
                    // HACK: because WWW is broken and sometimes just does not return STATUS in responseHeaders we are returning status code 200 (we can assume that status code is not an error since www.error is null or empty).
                    if (!www.responseHeaders.ContainsKey("STATUS"))
                    {
                        DebugLogger.LogWarning("Response headers doesn't contain status information. Since WWW marks response as one without errors, status code is set to 200 (OK).");
                        _statusCode = 200;
                    }
                    else
                    {
                        var status = www.responseHeaders["STATUS"];
                        DebugLogger.Log(string.Format("Response status: {0}", status));
                        var s = status.Split(' ');

                        if (s.Length >= 3 && int.TryParse(s[1], out _statusCode))
                        {
                            DebugLogger.Log(string.Format("Successfully parsed status code: {0}", _statusCode));
                        }
                        else
                        {
                            // HACK: Again, we can't parse the status code (it might be in some different format) - so we simply set it to 200.
                            DebugLogger.LogWarning(
                                "Unable to parse status code. Since WWW marks response as one without errors, status code is set to 200 (OK).");
                            _statusCode = 200;
                        }
                    }

                }
                catch (Exception e)
                {
                    _wasError = true;
                    _errorText = e.ToString();
                }
            }
            else
            {
                _wasTimeout = true;
            }

            yield return null;
        }

        public WrapRequest(string url)
        {
            _waitHandle = Utilities.UnityDispatcher.InvokeCoroutine(JobCoroutine(url));
            Address = new Uri(url);
        }

        public int Timeout { get; set; }

        public Uri Address { get; private set; }

        public IHttpWebResponse GetResponse()
        {
            _waitHandle.WaitOne();

            if (_wasTimeout)
            {
                throw new WebException("Timeout.", WebExceptionStatus.Timeout);
            }

            if (_wasError)
            {
                throw new WebException(_errorText);
            }

            return new WrapResponse(_data, _statusCode, ResponseEncoding);
        }
    }
}