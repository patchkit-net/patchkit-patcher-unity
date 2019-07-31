using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Utilities
{
    class Retry
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(Retry));
        
        private string _name;

        private int _times;

        private float _intervalSeconds;
        
        public Retry(string name = null)
        {
            _name = name;
            _times = 1;
            _intervalSeconds = 5;
        }

        public Retry Times(int count)
        {
            _times = count;
            return this;
        }

        public Retry IntervalSeconds(float seconds)
        {
            _intervalSeconds = seconds;
            return this;
        }

        public void Run(System.Action action)
        {
            int attempt = 1;
            while (CanRetry(attempt))
            {
                try
                {
                    action();
                    return;
                } 
                catch (System.OperationCanceledException)
                {
                    throw;
                }
                catch (System.Threading.ThreadInterruptedException)
                {
                    throw;
                }
                catch (System.Exception e)
                {
                    DebugLogger.LogWarning("Retry: Got error from action.");
                    
                    attempt++;
                    
                    if (CanRetry(attempt)) {
                        DebugLogger.LogException(e);
                        DebugLogger.Log("Will retry in " + _intervalSeconds + " seconds.");
                        System.Threading.Thread.Sleep((int) (_intervalSeconds * 1000));
                    } else {
                        DebugLogger.Log("Won't retry, tried " + attempt + " times already.");
                        throw e;
                    }
                    continue;
                }
            }
        }

        private bool CanRetry(int attempt) {
            return attempt <= _times - 1;
        }
    }
}