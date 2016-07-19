using System;
using UnityEngine;
using System.Net;

namespace PatchKit.Unity.Common
{
    internal static class InternetConnectionTester 
    {
        private const int Timeout = 10000;

        private const string TestingUrl = "http://www.google.com/";

        public static bool CheckInternetConnection(PatchKit.API.Async.AsyncCancellationToken cancellationToken)
        {
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(TestingUrl);

                webRequest.Timeout = Timeout;

                var webResponse = (HttpWebResponse)webRequest.GetResponse();

                if (webResponse.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            return false;
        }
    }
}
