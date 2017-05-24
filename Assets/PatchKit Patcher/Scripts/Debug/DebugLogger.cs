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

        private static string FormatMessage(string type, string message)
        {
            return string.Format("{0} {1}: {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"), type, message);
        }

        private static string FormatExceptionMessage(Exception exception)
        {
            return string.Format("{0}: {1}\n\nStack trace:\n{2}", exception.GetType(), exception.Message, exception.StackTrace);
        }

        // [LOG      ]
        // [WARNING  ]
        // [ERROR    ]
        // [EXCEPTION]

        public void Log(string message)
        {
            UnityEngine.Debug.Log(FormatMessage("[   Log   ]", message));
        }

        public void LogFormat(string message, params object[] args)
        {
            Log(string.Format(message, args));
        }

        public void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning(FormatMessage("[ Warning ]", message));
        }

        public void LogWarningFormat(string message, params object[] args)
        {
            LogWarning(string.Format(message, args));
        }

        public void LogError(string message)
        {
            UnityEngine.Debug.LogError(FormatMessage("[  Error  ]", message));
        }

        public void LogErrorFormat(string message, params object[] args)
        {
            LogError(string.Format(message, args));
        }

        public void LogException(Exception exception)
        {
            UnityEngine.Debug.LogErrorFormat(FormatMessage("[Exception]", FormatExceptionMessage(exception)));
            int innerExceptionCounter = 1;
            var innerException = exception.InnerException;
            while (innerException != null)
            {
                UnityEngine.Debug.LogErrorFormat("Inner Exception {0}: {1}", innerExceptionCounter, FormatExceptionMessage(innerException));
                innerException = innerException.InnerException;
            }
        }

        public void LogConstructor()
        {
            Log(string.Format("{0} constructor.", _context));
        }

        public void LogDispose()
        {
            Log(string.Format("{0} dispose.", _context));
        }

        public void LogVariable(object value, string name)
        {
            Log(string.Format("{0} = {1}", name, value));
        }
    }
}