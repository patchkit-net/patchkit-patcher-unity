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
            return new SentryMessage("Log GUID: " + logFileGuid);
        }
    }
}