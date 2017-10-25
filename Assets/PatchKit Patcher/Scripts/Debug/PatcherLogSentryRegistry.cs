using System;
using System.Net;
using SharpRaven;
using SharpRaven.Data;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogSentryRegistry
    {
        private readonly RavenClient _ravenClient;

        public PatcherLogSentryRegistry()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true; 
            
            _ravenClient
                = new RavenClient( 
                    "https://cb13d9a4a32f456c8411c79c6ad7be9d:90ba86762829401e925a9e5c4233100c@sentry.io/175617"); 
        }

        public void RegisterWithException(Exception exception, string logFileGuid)
        {
            AddDataToException(exception, logFileGuid);
            _ravenClient.Capture(new SentryEvent(exception));
        }

        private static void AddDataToException(Exception exception, string logFileGuid)
        {
            exception.Data.Add("log-guid", logFileGuid);
            exception.Data.Add("log-link", string.Format(
                "https://s3-us-west-2.amazonaws.com/patchkit-app-logs/patcher-unity/{0:yyyy_MM_dd}/{1}.201-log.gz", DateTime.Now, logFileGuid));

            var patcher = Patcher.Instance;
            if (patcher != null)
            {
                if(patcher.Data.Value.AppSecret != null)
                {
                    exception.Data.Add("app-secret", patcher.Data.Value.AppSecret);
                }
                if (patcher.LocalVersionId.Value.HasValue)
                {
                    exception.Data.Add("local-version", patcher.LocalVersionId.Value.ToString());
                }
                if (patcher.RemoteVersionId.Value.HasValue)
                {
                    exception.Data.Add("remote-version", patcher.RemoteVersionId.Value.ToString());
                }
            }

        }
    }
}