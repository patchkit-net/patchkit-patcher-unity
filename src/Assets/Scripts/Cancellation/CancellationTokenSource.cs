namespace PatchKit.Unity.Patcher.Cancellation
{
    internal class CancellationTokenSource
    {
        public bool IsCancelled { get; private set; }

        public CancellationTokenSource()
        {
            IsCancelled = false;
        }

        public void Cancel()
        {
            IsCancelled = true;
        }

        public CancellationToken Token
        {
            get { return new CancellationToken(this); }
        }

        public static implicit operator CancellationToken(CancellationTokenSource cancellationTokenSource)
        {
            return cancellationTokenSource.Token;
        }
    }
}