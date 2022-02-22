using System;
using System.Threading;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Utilities
{
    public static class ThreadingPool
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(ThreadingPool));

        private static Semaphore _pool = new Semaphore(16, 17);
        private static ManualResetEvent _awaiter = new ManualResetEvent(false);

        public static void ThreadingPoolExecute(CancellationToken cancellationToken, Action action)
        {
            _pool.WaitOne();
            ThreadPool.QueueUserWorkItem(state => ThreadingPoolProc(cancellationToken, action));
        }

        private static void ThreadingPoolProc(CancellationToken cancellationToken, Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                DebugLogger.LogError(e.ToString());
                throw;
            }
            finally
            {
                int release = _pool.Release();
                if (release == 16 || cancellationToken.IsCancelled)
                {
                    _awaiter.Set();
                }
            }
        }

        public static void WaitOne(CancellationToken cancellationToken)
        {
            var release = _pool.Release();
            if (release == 16 || cancellationToken.IsCancelled)
            {
                _awaiter.Set();
            }

            _awaiter.WaitOne();
        }
    }
}