using System;
using System.Threading;
using UnityEngine;
using PatchKit.Api;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class WrapRequest : IHttpWebRequest
    {
        public class WWWJob : PatcherLocalJobs.ILocalJob
        {
            public WWWJob(string url)
            {
                _url = url;
                isDone = false;
            }

            private string _url;
            private WWW _www;

            private string _downloadedData;

            public bool isDone { get; private set; }

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
            }

            public WrapResponse MakeResponse()
            {
                return new WrapResponse(_downloadedData, "iso-8859-2");
            }
        }

        public WrapRequest(string url)
        {
            _job = new WWWJob(url);
            PatcherLocalJobs.instance.ScheduleJob(_job);

            while (!_job.isDone) { Thread.Sleep(1000); }

            _uri = new Uri(url);
        }

        private WWWJob _job;
        private Uri _uri;

        public int Timeout { get; set; }

        public Uri Address { get {
            return _uri;
        }}

        public IHttpWebResponse GetResponse()
        {
            return _job.MakeResponse();
        }
    }
}