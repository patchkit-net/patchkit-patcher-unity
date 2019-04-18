using UnityEngine;

public partial class Patcher
{
    private async Task Quit2()
    {
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
    }
}