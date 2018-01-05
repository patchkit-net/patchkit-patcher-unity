using System;
using System.Net;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class DownloadServerErrorException : Exception
    {
        public DownloadServerErrorException(string url, HttpStatusCode statusCode) : base(string.Format("Download server has experienced some issues while requesting {0} which resulted in {1} status code.", url, statusCode))
        {
        }
    }
}