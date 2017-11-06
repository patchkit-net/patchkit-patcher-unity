using System;
using System.Collections;
using System.Net;
using System.Threading;
using UnityEngine;
using PatchKit.Api;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class WrapRequest : IHttpWebRequest
    {
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

                if (!TryParseStatusCode(www.responseHeaders["STATUS"], out _statusCode))
                {
                    _wasError = true;
                    _errorText = "Couldn't parse status code.";
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

        private static bool TryParseStatusCode(string status, out int statusCode)
        {
            statusCode = 0;

            var s = status.Split(' ');

            return s.Length >= 3 && int.TryParse(s[1], out statusCode);
        }
    }
}