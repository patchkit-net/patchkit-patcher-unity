using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

public partial class Patcher
{
    [NotNull]
    private Task Quit2()
    {
        Debug.Log(message: "Quitting...");

        ModifyState(x: () => State.Kind = PatcherStateKind.Quitting);

#if UNITY_EDITOR
        if (Application.isEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
        else
#endif
        {
            Application.Quit();
        }

        Debug.Log(message: "Quitting finished.");

        return Task.CompletedTask;
    }
}