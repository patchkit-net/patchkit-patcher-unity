using System;
using JetBrains.Annotations;

namespace PatchKit.Unity.Patcher.Cancellation
{
    internal struct CancellationToken
    {
        [CanBeNull] private readonly CancellationTokenSource _cancellationTokenSource;

        public static readonly CancellationToken Empty = new CancellationToken(null);

        internal CancellationToken(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        public event Action Cancelled
        {
            add
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancelled += value;
                }
            }
            remove
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancelled -= value;
                }
            }
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