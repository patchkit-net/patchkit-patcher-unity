using System;

namespace PatchKit.Unity.Patcher.Debug
{
    public class DebugLogger
    {
        private readonly string _context;

        public DebugLogger(Type context)
        {
            _context = context.FullName;
        }

        // TODO: Unify logging format and add date time.

        private static string FormatExceptionLog(Exception exception)
        {
            return string.Format("{0}\n\nStack trace:\n{1}", exception.Message, exception.StackTrace);
        }

        public void Log(object message)
        {
            UnityEngine.Debug.LogFormat("[{0}] {1}", _context, message);
        }

        public void LogConstructor()
        {
            UnityEngine.Debug.LogFormat("[{0}] Constructor.", _context);
        }

        public void LogVariable(object value, string name)
        {
            UnityEngine.Debug.LogFormat("[{0}] {1} = {2}", _context, name, value);
        }

        public void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarningFormat("[{0}] {1}", _context, message);
        }

        public void LogError(object message)
        {
            UnityEngine.Debug.LogErrorFormat("[{0}] {1}", _context, message);
        }

        public void LogException(Exception exception)
        {
            UnityEngine.Debug.LogErrorFormat("[{0}] Exception: {1}", _context, FormatExceptionLog(exception));
            int innerExceptionCounter = 1;
            var innerException = exception.InnerException;
            while (innerException != null)
            {
                UnityEngine.Debug.LogErrorFormat("[{0}] Inner Exception {1}: {2}", _context,
                    innerExceptionCounter, FormatExceptionLog(exception));
                innerException = innerException.InnerException;
            }
        }
    }
}