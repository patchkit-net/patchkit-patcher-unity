using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
#pragma warning disable 1998
    public async Task CancelUpdateAppAsync()
#pragma warning restore 1998
    {
        try
        {
            if (_hasAppUpdateTask)
            {
                Assert.IsNotNull(value: _appUpdateTaskCts);

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