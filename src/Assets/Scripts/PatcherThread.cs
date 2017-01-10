using System;
using System.Threading;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    internal class PatcherThread : IDisposable
    {
        private readonly AppUpdater _appUpdater;

        private CancellationTokenSource _cancellationTokenSource;

        private Thread _thread;

        private bool _startPatchingCalled;

        public PatcherThread(AppUpdater appUpdater)
        {
            _appUpdater = appUpdater;
        }

        public event Action<Exception> Finished;

        public bool IsPatching
        {
            get { return _thread != null && _thread.IsAlive; }
        }

        public void StartPatching()
        {
            AssertChecks.MethodCalledOnlyOnce(ref _startPatchingCalled, "Patch");
            Assert.IsFalse(IsPatching);

            _cancellationTokenSource = new CancellationTokenSource();
            _thread = new Thread(Patch);
            _thread.Start();
        }

        public void CancelPatching()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        public void AbortPatching()
        {
            if (_thread != null && _thread.IsAlive)
            {
                _thread.Abort();
            }
        }

        public void Dispose()
        {
            AbortPatching();
        }

        private void Patch()
        {
            Exception exception = null;

            try
            {
                _appUpdater.Patch(_cancellationTokenSource.Token);
            }
            catch (Exception e)
            {
                exception = e;
            }

            OnFinished(exception);
        }

        protected virtual void OnFinished(Exception exception)
        {
            if (Finished != null) Finished(exception);
        }
    }
}