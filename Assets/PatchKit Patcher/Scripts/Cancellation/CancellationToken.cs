using System;
using JetBrains.Annotations;

namespace PatchKit.Unity.Patcher.Cancellation
{
    public struct CancellationToken
    {
        [CanBeNull] private readonly CancellationTokenSource _cancellationTokenSource;

        public static readonly CancellationToken Empty = new CancellationToken(null);

        public CancellationToken(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        public CancellationTokenRegistration Register(Action action)
        {
            return new CancellationTokenRegistration(_cancellationTokenSource, action);
        }

        public bool IsCancelled
        {
            get { return _cancellationTokenSource != null && _cancellationTokenSource.IsCancelled; }
        }

        public void ThrowIfCancellationRequested()
        {
            if (IsCancelled)
            {
                throw new OperationCanceledException();
            }
        }
    }
}