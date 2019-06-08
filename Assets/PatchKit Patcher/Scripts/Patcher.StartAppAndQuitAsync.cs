using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private async Task<bool> StartAppAsync()
    {
        if (!await StartAppAsync())
        {
            return false;
        }

        return await QuitAsync();
    }
}