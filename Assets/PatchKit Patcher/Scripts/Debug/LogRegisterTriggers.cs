using System;
using PatchKit.Apps.Updating.Debug;
using UniRx;

namespace PatchKit.Patching.Unity.Debug
{
    public class LogRegisterTriggers : IDisposable
    {
        private readonly Subject<Exception> _exceptionTrigger
            = new Subject<Exception>();
        
        public IObservable<Exception> ExceptionTrigger
        {
            get { return _exceptionTrigger; }
        }

        public LogRegisterTriggers()
        {
            DebugLogger.ExceptionOccured += OnExceptionOccured;
        }

        public void Dispose()
        {
            DebugLogger.ExceptionOccured -= OnExceptionOccured;
        }
        
        private void OnExceptionOccured(Exception exception)
        {
            _exceptionTrigger.OnNext(exception);
        }
    }
}