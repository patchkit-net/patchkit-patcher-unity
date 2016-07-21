using System;
using UnityEngine;

namespace PatchKit.Unity.Utilities
{
    public static class AsyncUtilities
    {
        private class AsyncWaitYieldInstruction : CustomYieldInstruction
        {
            private readonly IAsyncResult _asyncResult;

            public AsyncWaitYieldInstruction(IAsyncResult asyncResult)
            {
                _asyncResult = asyncResult;
            }

            public override bool keepWaiting
            {
                get { return !_asyncResult.IsCompleted; }
            }
        }

        public static CustomYieldInstruction WaitCoroutine(this IAsyncResult @this)
        {
            return new AsyncWaitYieldInstruction(@this);
        }
    }
}