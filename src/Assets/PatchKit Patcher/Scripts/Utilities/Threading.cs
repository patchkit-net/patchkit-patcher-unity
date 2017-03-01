using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace PatchKit.Unity.Utilities
{
    public static class Threading
    {
        /// <summary>
        /// Starts the thread with specified action in coroutine.
        /// </summary>
        /// <param name="action">The action to do in thread.</param>
        /// <param name="onSuccess">The action performed after successful thread result.</param>
        /// <param name="onFailed">The action performed after thread failure.</param>
        public static IEnumerator StartThreadCoroutine<T>(Func<T> action, Action<T> onSuccess, Action<Exception> onFailed = null)
        {
            bool success = false;
            T result = default(T);
            Exception exception = null;

            Thread thread = new Thread(() =>
            {
                try
                {
                    result = action();
                    success = true;
                }
                catch (Exception e)
                {
                    exception = e;
                }
            });

            try
            {
                thread.Start();

                while (thread.IsAlive)
                {
                    yield return null;
                }

                if (success)
                {
                    onSuccess(result);
                }

                if (exception != null)
                {
                    if (onFailed == null)
                    {
                        Debug.LogException(exception);
                    }
                    else
                    {
                        onFailed(exception);
                    }
                }
            }
            finally
            {
                thread.Abort();
            }
        }
    }
}