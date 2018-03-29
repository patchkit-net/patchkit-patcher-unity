using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;


namespace PatchKit.Unity.Patcher
{
    [CustomEditor(typeof(Patching.Unity.Patcher))]
    public class PatcherEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            Patching.Unity.Patcher patcher = (Patching.Unity.Patcher) target;

            if (patcher.EditorAppSecret != Patching.Unity.Patcher.EditorAllowedSecret)
            {
                EditorGUILayout.HelpBox("Reset the editor app secret before building.", MessageType.Warning);
                if(GUILayout.Button("Reset"))
                {
                    patcher.EditorAppSecret = Patching.Unity.Patcher.EditorAllowedSecret;
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
        }
    }
}