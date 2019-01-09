using PatchKit.Apps.Updating.Debug;
using UniRx;

namespace PatchKit.Patching.Unity.Debug
{
    public class LogRegisterTriggers : System.IDisposable
    {
        private readonly Subject<System.Exception> _exceptionTrigger
            = new Subject<System.Exception>();
        
        public IObservable<System.Exception> ExceptionTrigger
        {
            get { return _exceptionTrigger; }
        }

        public LogRegisterTriggers()
        {
        }

        public void Dispose()
        {
        }
        
        private void OnExceptionOccured(System.Exception exception)
        {
            _exceptionTrigger.OnNext(exception);
        }
    }
}