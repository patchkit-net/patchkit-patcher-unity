using System;
using System.Collections;
using System.Threading;
using PatchKit.Api;
using UnityEngine;

namespace PatchKit.Unity.Utilities
{
    public static class ApiConnectionExtensions
    {
        public static IEnumerator GetCoroutine(this ApiConnection @this, string path,
            string query, Action<IApiResponse> onSuccess, Action<Exception> onFailed = null)
        {
            IApiResponse apiResponse = null;
            Exception exception = null;

            Thread thread = new Thread(() =>
            {
                try
                {
                    apiResponse = @this.Get(path, query);
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

                if (apiResponse != null)
                {
                    onSuccess(apiResponse);
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