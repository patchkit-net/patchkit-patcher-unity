using System;
using UniRx;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogRegisterTriggers : IDisposable
    {
        private readonly Subject<Exception> _exceptionTrigger
            = new Subject<Exception>();
        
        public IObservable<Exception> ExceptionTrigger
        {
            get { return _exceptionTrigger; }
        }

        public PatcherLogRegisterTriggers()
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