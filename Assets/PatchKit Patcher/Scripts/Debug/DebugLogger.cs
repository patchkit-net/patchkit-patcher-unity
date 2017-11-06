using System;
using PatchKit.Logging;

namespace PatchKit.Unity.Patcher.Debug
{
    public class DebugLogger
    {
        private readonly string _context;
        private readonly ILogger _logger;

        public DebugLogger(Type context)
        {
            _context = context.FullName;
            _logger = PatcherLogManager.DefaultLogger;
        }

        [IgnoreLogStackTrace]
        public void Log(string message)
        {
            _logger.LogDebug(message);
        }

        [IgnoreLogStackTrace]
        public void LogFormat(string message, params object[] args)
        {
            Log(string.Format(message, args));
        }

        [IgnoreLogStackTrace]
        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        [IgnoreLogStackTrace]
        public void LogWarningFormat(string message, params object[] args)
        {
            LogWarning(string.Format(message, args));
        }

        [IgnoreLogStackTrace]
        public void LogError(string message)
        {
            _logger.LogError(message);
        }

        [IgnoreLogStackTrace]
        public void LogErrorFormat(string message, params object[] args)
        {
            LogError(string.Format(message, args));
        }

        [IgnoreLogStackTrace]
        public void LogException(Exception exception)
        {
            _logger.LogError("An exception has occured", exception);
            OnExceptionOccured(exception);
        }

        [IgnoreLogStackTrace]
        public void LogConstructor()
        {
            Log(string.Format("{0} constructor.", _context));
        }

        [IgnoreLogStackTrace]
        public void LogDispose()
        {
            Log(string.Format("{0} dispose.", _context));
        }

        [IgnoreLogStackTrace]
        public void LogVariable(object value, string name)
        {
            Log(string.Format("{0} = {1}", name, value));
        }

        public static event Action<Exception> ExceptionOccured;

        private static void OnExceptionOccured(Exception obj)
        {
            var handler = ExceptionOccured;
            if (handler != null)
            {
                handler(obj);
            }
        }
    }
}