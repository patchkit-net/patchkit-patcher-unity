using System;
using System.Net;
using PatchKit.IssueReporting;
using SharpRaven;
using SharpRaven.Data;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogSentryRegistry
    {
        public RavenClient RavenClient
        {
            get
            {
                return _ravenClient;
            }
        }

        private readonly RavenClient _ravenClient;

        public static readonly string RavenClientId = "https://cb13d9a4a32f456c8411c79c6ad7be9d:90ba86762829401e925a9e5c4233100c@sentry.io/175617";

        public PatcherLogSentryRegistry()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;

            _ravenClient = new RavenClient(RavenClientId);
        }

        public void RegisterWithException(Issue issue, string logFileGuid)
        {
            var sentryEvent = new SentryEvent(issue.Exception)
            {
                Tags = issue.Tags
            };
            if (issue.Message != null)
            {
                sentryEvent.Message = issue.Message;
            }
            AddDataToSentryEvent(sentryEvent, logFileGuid);
            _ravenClient.Capture(sentryEvent);
        }

        public void RegisterWithException(Exception exception, string logFileGuid)
        {
            RegisterWithException(new Issue()
            {
                Exception = exception
            }, logFileGuid);
        }

        public static void AddDataToSentryEvent(SentryEvent sentryEvent, string logFileGuid)
        {
            sentryEvent.Exception.Data.Add("log-guid", logFileGuid);
            sentryEvent.Exception.Data.Add("log-link", string.Format(
                "https://s3-us-west-2.amazonaws.com/patchkit-app-logs/patcher-unity/{0:yyyy_MM_dd}/{1}.201-log.gz", DateTime.Now, logFileGuid));

            var patcher = Patcher.Instance;
            if (patcher != null)
            {
                if(patcher.Data.Value.AppSecret != null)
                {
                    sentryEvent.Tags.Add("app-secret", patcher.Data.Value.AppSecret);
                }
                if (patcher.LocalVersionId.Value.HasValue)
                {
                    sentryEvent.Exception.Data.Add("local-version", patcher.LocalVersionId.Value.ToString());
                }
                if (patcher.RemoteVersionId.Value.HasValue)
                {
                    sentryEvent.Exception.Data.Add("remote-version", patcher.RemoteVersionId.Value.ToString());
                }

                sentryEvent.Tags.Add("system-info", EnvironmentInfo.GetSystemInformation());
                sentryEvent.Tags.Add("patcher-version", Version.Value);
            }
        }

    }
}