using System;
using System.IO;
using PatchKit.Api;
using PatchKit.Network;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class WrapResponse : IHttpResponse
    {
        private readonly string _data;
        private readonly int _statusCode;
        private MemoryStream _contentStream;

        public WrapResponse(string data, int statusCode, string charset)
        {
            if (data == null) throw new ArgumentNullException("data");
            
            _data = data;
            _statusCode = statusCode;
            CharacterSet = charset;
        }

        public Stream ContentStream
        {
            get
            {
                if (_contentStream != null)
                {
                    return _contentStream;
                }
                
                _contentStream = new MemoryStream();
                var writer = new StreamWriter(_contentStream);
                
                writer.Write(_data);
                writer.Flush();
                _contentStream.Position = 0;
                return _contentStream;
            }
        }
        
        public string CharacterSet { get; private set; }

        public System.Net.HttpStatusCode StatusCode
        {
            get { return (System.Net.HttpStatusCode) _statusCode; }
        }

        public void Dispose()
        {
            if (_contentStream != null)
            {
                _contentStream.Dispose();
            }
        }
    }
}