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

        public static void Log(this IDebugLogger @this, object message)
        {
            Debug.Log(string.Format("[{0}] {1}", GetTypeName(@this), message));
        }
    }
}
