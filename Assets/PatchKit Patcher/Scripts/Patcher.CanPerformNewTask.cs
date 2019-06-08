using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

public partial class Patcher
{
    private bool CanPerformNewTask()
    {
        return _isInitialized && 
            !_hasInitializeTask &&
            !_hasQuit && 
            !_hasQuitTask;
    }
}