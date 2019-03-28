using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using PatchKit.Network;

namespace PatchKit.Api
{
    internal class ApiResponse : IApiResponse
    {
        public ApiResponse(IHttpResponse httpResponse)
        {
            HttpResponse = httpResponse;

            var responseStream = HttpResponse.ContentStream;

            if (HttpResponse.CharacterSet == null || responseStream == null)
            {
                throw new WebException("Invalid response from API server.");
            }

            var responseEncoding = Encoding.GetEncoding(HttpResponse.CharacterSet);

            using (var streamReader = new StreamReader(responseStream, responseEncoding))
            {
                Body = streamReader.ReadToEnd();
            }
        }

        public IHttpResponse HttpResponse { get; private set; }

        public string Body { get; private set; }

        public JToken GetJson()
        {
            return JToken.Parse(Body);
        }

        void IDisposable.Dispose()
        {
            HttpResponse.Dispose();
        }
    }
}
