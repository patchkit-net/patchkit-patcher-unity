using System;
using System.Collections.Generic;
using System.Net;
using SharpRaven;
using SharpRaven.Data;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogSender : MonoBehaviour
    {
        private RavenClient _ravenClient;

        private void Awake()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
            
            _ravenClient =
                new RavenClient(
                    "https://cb13d9a4a32f456c8411c79c6ad7be9d:90ba86762829401e925a9e5c4233100c@sentry.io/175617");

            _ravenClient.ErrorOnCapture = UnityEngine.Debug.LogException;
            
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            DebugLogger.ExceptionOccured += OnExceptionOccured;
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
            DebugLogger.ExceptionOccured -= OnExceptionOccured;
        }

        private void OnExceptionOccured(Exception exception)
        {
            _ravenClient.Capture(new SentryEvent(exception));
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            _ravenClient.AddTrail(new Breadcrumb("log")
            {
                Message = condition,
                Level = LogTypeToBreadcrumbLevel(type),
                Data = new Dictionary<string, string>()
                {
                    {"stack_trace", stackTrace}
                }
            });
        }

        private BreadcrumbLevel LogTypeToBreadcrumbLevel(LogType logType)
        {
            switch (logType)
            {
                case LogType.Error:
                    return BreadcrumbLevel.Error;
                case LogType.Assert:
                    return BreadcrumbLevel.Critical;
                case LogType.Warning:
                    return BreadcrumbLevel.Warning;
                case LogType.Log:
                    return BreadcrumbLevel.Info;
                case LogType.Exception:
                    return BreadcrumbLevel.Error;
                default:
                    throw new ArgumentOutOfRangeException("logType", logType, null);
            }
        }
    }
}