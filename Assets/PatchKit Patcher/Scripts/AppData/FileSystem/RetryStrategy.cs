using System;
using System.IO;
using System.Threading;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher.AppData.FileSystem
{
    public class RetryStrategy : IRequestRetryStrategy
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(RetryStrategy));

        public const int DefaultTryCount = 10;
        public const int DefaultDelayMsec = 500;

        private readonly int _tryCount;
        private int _currentTry = 0;
        private readonly int _delay;

        private RetryStrategy()
            : this(DefaultTryCount, DefaultDelayMsec)
        {
        }

        public RetryStrategy(int tryCount, int delayBetweenEachTryMsec)
        {
            _tryCount = tryCount;
            _delay = delayBetweenEachTryMsec;
        }

        public void OnRequestSuccess()
        {
            // Do nothing
        }

        public void OnRequestFailure()
        {
            _currentTry++;
        }

        public int DelayBeforeNextTry {
            get
            {
                return _delay;
            }
        }

        public bool ShouldRetry
        {
            get
            {
                return _currentTry <= _tryCount;
            }
        }

        public static void TryExecute(Action action, IRequestRetryStrategy retryStrategy, PatchKit.Unity.Patcher.Cancellation.CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException e)
                {
                    retryStrategy.OnRequestFailure();

                    if (!retryStrategy.ShouldRetry)
                    {
                        DebugLogger.LogError(string.Format("An IO Exception has occured: {0}. rethrowing.", e));
                        throw;
                    }

                    DebugLogger.LogWarning(string.Format("An IO Exception has occured: {0}. retrying...", e));
                    Threading.CancelableSleep(retryStrategy.DelayBeforeNextTry, cancellationToken);
                }
            } while (retryStrategy.ShouldRetry);
        }

        public static void TryExecute(Action action, PatchKit.Unity.Patcher.Cancellation.CancellationToken cancellationToken)
        {
            TryExecute(action, new RetryStrategy(), cancellationToken);
        }
    }
}