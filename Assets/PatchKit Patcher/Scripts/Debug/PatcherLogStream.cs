using System;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogStream : IDisposable
    {
        private readonly Subject<PatcherLogMessage> _messages
            = new Subject<PatcherLogMessage>();

        public UniRx.IObservable<PatcherLogMessage> Messages
        {
            get { return _messages; }
        }

        public void Dispose()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            _messages.OnNext(new PatcherLogMessage
            {
                Message = condition,
                StackTrace = stackTrace,
                LogType = type
            });
        }
    }
}