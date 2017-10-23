using System;
using System.IO;
using UnityEngine;
using PatchKit.Api;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class WrapResponse : IHttpWebResponse
    {
        public WrapResponse(string data, string charset)
        {
            CharacterSet = charset;
            _data = data;
        }

        private string _data;

        public string CharacterSet { get; private set; }

        public System.Net.HttpStatusCode StatusCode
        {
            get
            {
                return System.Net.HttpStatusCode.OK;
            }
        }

        public System.IO.Stream GetResponseStream()
        {
            if (_data == null)
            {
                throw new NullReferenceException();
            }

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);

            writer.Write(_data);
            writer.Flush();
            stream.Position = 0;

            return stream;
        }
    }
}