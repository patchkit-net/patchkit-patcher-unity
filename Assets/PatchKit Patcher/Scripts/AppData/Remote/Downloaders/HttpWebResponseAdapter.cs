using System;
using System.IO;
using System.Net;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class HttpWebResponseAdapter : IHttpWebResponseAdapter
    {
        private readonly HttpWebResponse _httpWebResponse;

        public HttpWebResponseAdapter(HttpWebResponse httpWebResponse)
        {
            _httpWebResponse = httpWebResponse;
        }

        public void Dispose()
        {
            ((IDisposable) _httpWebResponse).Dispose();
        }

        public HttpStatusCode StatusCode
        {
            get { return _httpWebResponse.StatusCode; }
        }

        public Stream GetResponseStream()
        {
            return _httpWebResponse.GetResponseStream();
        }
    }
}