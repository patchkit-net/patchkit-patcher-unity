using System;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogStream : IDisposable
    {
        private readonly Subject<string> _messages
            = new Subject<string>();

        public UniRx.IObservable<string> Messages
        {
            get { return _messages; }
        }

        public PatcherLogStream()
        {
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }
        
        public void Dispose()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            _messages.OnNext(condition);
        }
    }
}