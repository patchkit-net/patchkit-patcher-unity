using System;

namespace PatchKit.Unity.Patcher.Cancellation
{
    public class CancellationTokenSource
    {
        public bool IsCancelled { get; private set; }

        public CancellationTokenSource()
        {
            IsCancelled = false;
        }

        public void Cancel()
        {
            if (!IsCancelled)
            {
                IsCancelled = true;

                if (Cancelled != null)
                {
                    Cancelled();
                }
            }
        }

        public CancellationToken Token
        {
            get { return new CancellationToken(this); }
        }

        public event Action Cancelled;

        public static implicit operator CancellationToken(CancellationTokenSource cancellationTokenSource)
        {
            return cancellationTokenSource.Token;
        }
    }
}