using System.Net;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class HttpWebRequestAdapter : IHttpWebRequestAdapter
    {
        private readonly HttpWebRequest _httpWebRequest;

        public HttpWebRequestAdapter(HttpWebRequest httpWebRequest)
        {
            Checks.ArgumentNotNull(httpWebRequest, "httpWebRequest");

            _httpWebRequest = httpWebRequest;
        }

        public IHttpWebResponseAdapter GetResponse()
        {
            return new HttpWebResponseAdapter((HttpWebResponse)_httpWebRequest.GetResponse());
        }

        public string Method
        {
            get { return _httpWebRequest.Method; }
            set { _httpWebRequest.Method = value; }
        }

        public int Timeout
        {
            get { return _httpWebRequest.Timeout; }
            set { _httpWebRequest.Timeout = value; }
        }

        public void AddRange(long start, long end)
        {
            _httpWebRequest.AddRange(start, end);
        }
    }
}