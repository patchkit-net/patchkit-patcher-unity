using System;

namespace PatchKit.Unity.Patcher.Debug
{
    internal static class DebugLogger
    {
        private static string GetTypeName(object obj)
        {
            return obj.GetType().Name;
        }

        private static string FormatExceptionLog(Exception exception)
        {
            return string.Format("{0}\n\nStack trace:\n{1}", exception.Message, exception.StackTrace);
        }

        public static void Log(object context, object message)
        {
            UnityEngine.Debug.LogFormat("[{0}] {1}", GetTypeName(context), message);
        }

        public static void LogException(object context, Exception exception)
        {
            UnityEngine.Debug.LogErrorFormat("[{0}] Exception: {1}", GetTypeName(context), FormatExceptionLog(exception));
            int innerExceptionCounter = 1;
            var innerException = exception.InnerException;
            while (innerException != null)
            {
                UnityEngine.Debug.LogErrorFormat("[{0}] Inner Exception {1}: {2}", GetTypeName(context),
                    innerExceptionCounter, FormatExceptionLog(exception));
                innerException = innerException.InnerException;
            }
        }

        public static void LogWarning(object context, object message)
        {
            UnityEngine.Debug.LogFormat("[{0}] Warning: {1}", GetTypeName(context), message);
        }
    }
}