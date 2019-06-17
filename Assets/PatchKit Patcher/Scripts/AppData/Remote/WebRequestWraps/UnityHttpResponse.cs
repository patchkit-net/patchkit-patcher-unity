using System;
using System.IO;
using System.Text;
using PatchKit.Network;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class UnityHttpResponse : IHttpResponse
    {
        private readonly string _data;
        private readonly System.Net.HttpStatusCode _statusCode;
        private MemoryStream _contentStream;

        public UnityHttpResponse(string data, System.Net.HttpStatusCode statusCode)
        {
            if (data == null) throw new ArgumentNullException("data");
            
            _data = data;
            _statusCode = statusCode;
            CharacterSet = Encoding.UTF8.WebName;
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
            get { return _statusCode; }
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