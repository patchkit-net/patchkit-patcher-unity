using System;
using System.Threading;
using UnityEngine;
using PatchKit.Api;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class WrapRequest : IHttpWebRequest
    {
        public const string responseEncoding = "iso-8859-2";

        public class WWWJob : PatcherLocalJobs.ILocalJob
        {
            public WWWJob(string url)
            {
                _url = url;
                isDone = false;
                error = null;
            }

            private string _url;
            private WWW _www;

            private string _downloadedData;

            public bool isDone { get; private set; }

            public string error { get; private set; }

            public void OnStart()
            {
                _www = new WWW(_url);
            }

            public void OnFinished()
            {
                isDone = true;
                _downloadedData = _www.text;
            }

            public void Update()
            {
                if (_www.isDone)
                {
                    isDone = true;
                }

                if (!string.IsNullOrEmpty(_www.error))
                {
                    isDone = true;
                    error = _www.error;
                }
            }

            public WrapResponse MakeResponse()
            {
                return new WrapResponse(_downloadedData, responseEncoding);
            }
        }

        public WrapRequest(string url)
        {
            _job = new WWWJob(url);
            _uri = new Uri(url);

            PatcherLocalJobs.instance.ScheduleJob(_job);
        }

        private WWWJob _job;
        private Uri _uri;

        public int Timeout { get; set; }

        public Uri Address 
        { 
            get 
            {
                return _uri;
            }
        }

        public IHttpWebResponse GetResponse()
        {
            var start = DateTime.Now;
            Func<bool> isTimeout = () => DateTime.Now.Subtract(start).Milliseconds > Timeout;

            while (!_job.isDone || isTimeout())
            {
                Thread.Sleep(100);
            }

            if (isTimeout())
            {
                throw new TimeoutException();
            }

            if (_job.error != null)
            {
                throw new Exception(_job.error);
            }

            return _job.MakeResponse();
        }
    }
}