using System;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Log
{
    // ReSharper disable once InconsistentNaming
    internal static class IDebugLoggerExtensions
    {
        private static string GetTypeName(object obj)
        {
            return obj.GetType().Name;
        }

        private static string FormatExceptionLog(Exception exception)
        {
            return string.Format("{0}\n\nStack trace:\n{1}", exception.Message, exception.StackTrace);
        }

        public static void Log(this IDebugLogger @this, object message)
        {
            Debug.LogFormat("[{0}] {1}", GetTypeName(@this), message);
        }

        public static void LogException(this IDebugLogger @this, Exception exception)
        {
            Debug.LogErrorFormat("[{0}] Exception: {1}", GetTypeName(@this), FormatExceptionLog(exception));
            int innerExceptionCounter = 1;
            var innerException = exception.InnerException;
            while (innerException != null)
            {
                Debug.LogErrorFormat("[{0}] Inner Exception {1}: {2}", GetTypeName(@this), innerExceptionCounter, FormatExceptionLog(exception));
                innerException = innerException.InnerException;
            }
        }

        public static void LogWarning(this IDebugLogger @this, object message)
        {
            Debug.LogFormat("[{0}] Warning: {1}", GetTypeName(@this), message);
        }
    }
}
