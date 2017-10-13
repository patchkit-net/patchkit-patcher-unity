using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using PatchKit.Api;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class WrapRequest : IHttpWebRequest
    {
        public const string responseEncoding = "iso-8859-2";

        private EventWaitHandle _waitHandle;

        public string Data { get; private set; }
        public bool WasError { get; private set; }

        IEnumerator JobCoroutine(string url)
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

            if (www.isDone)
            {
                Data = www.text;
            }
            else if (!string.IsNullOrEmpty(www.error))
            {
                WasError = true;
                Data = www.error;
            }
            else
            {
                WasError = true;
                Data = "Timeout exception";
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

            if (WasError)
            {
                throw new Exception(Data);
            }

            return new WrapResponse(Data, responseEncoding);
        }
    }
}