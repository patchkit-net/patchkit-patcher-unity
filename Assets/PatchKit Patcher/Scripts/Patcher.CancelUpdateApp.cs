using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    public async Task CancelUpdateAppAsync()
    {
        try
        {
            if (_hasAppUpdateTask)
            {
                Debug.Log(message: "Cancelling updating app...");

                _appUpdateTaskCts.Cancel();

                Debug.Log(message: "Successfully cancelled updating app.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                message: "Failed to cancel update app: unknown error.");
            Debug.LogException(exception: e);
        }
    }
}
