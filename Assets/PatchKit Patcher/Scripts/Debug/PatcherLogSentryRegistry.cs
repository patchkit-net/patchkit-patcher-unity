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
            _ravenClient.Capture(new SentryEvent(exception)
            {
                Message = CreateSentryMessage(logFileGuid)
            });
        }

        private static SentryMessage CreateSentryMessage(string logFileGuid)
        {
            var logFileLink = string.Format(
                "https://s3-us-west-2.amazonaws.com/patchkit-app-logs/patcher-unity/2017_04_03/{0}.201-log.gz", logFileGuid);


            var appSecret = "{no secret}";
            var patcher = Patcher.Instance;
            if (patcher != null && patcher.Data.Value.AppSecret != null)
            {
                appSecret = Patcher.Instance.Data.Value.AppSecret;
            }

            var msg = string.Format("Log: {0}\nApp secret: {1}", logFileLink, appSecret);

            return new SentryMessage(msg);
        }
    }
}