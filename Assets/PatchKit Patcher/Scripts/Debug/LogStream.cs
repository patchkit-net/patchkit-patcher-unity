using UniRx;
using UnityEngine;

namespace PatchKit.Patching.Unity.Debug
{
    public class LogStream : System.IDisposable
    {
        private readonly Subject<string> _messages
            = new Subject<string>();

        public IObservable<string> Messages
        {
            get { return _messages; }
        }

        public LogStream()
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