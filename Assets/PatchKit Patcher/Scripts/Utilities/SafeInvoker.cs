using System;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Utilities
{
    public static class SafeInvoker
    {
        public static void Invoke(Action action, Action onSucessAction = null, Action<Exception> onFailedAction = null)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                if (onFailedAction != null)
                {
                    onFailedAction(exception);
                }
            }

            if (onSucessAction != null)
            {
                onSucessAction();
            }
        }
    }
}
