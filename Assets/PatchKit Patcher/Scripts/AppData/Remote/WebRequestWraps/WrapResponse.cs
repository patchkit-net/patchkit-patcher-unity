using System;
using System.IO;
using PatchKit.Api;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class WrapResponse : IHttpWebResponse
    {
        private readonly string _data;
        private readonly int _statusCode;

        public WrapResponse(string data, int statusCode, string charset)
        {
            _data = data;
            _statusCode = statusCode;
            CharacterSet = charset;
        }

        public string CharacterSet { get; private set; }

        public System.Net.HttpStatusCode StatusCode
        {
            get { return (System.Net.HttpStatusCode) _statusCode; }
        }

        public Stream GetResponseStream()
        {
            if (_data == null)
            {
                throw new NullReferenceException();
            }

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);

            writer.Write(_data);
            writer.Flush();
            stream.Position = 0;

            return stream;
        }
    }
}