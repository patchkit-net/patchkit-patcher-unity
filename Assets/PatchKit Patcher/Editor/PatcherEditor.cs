using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;


namespace PatchKit.Unity.Patcher
{
    [CustomEditor(typeof(Patcher))]
    public class PatcherEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            Patcher patcher = (Patcher) target;

            if (patcher.EditorAppSecret != Patcher.EditorAllowedSecret)
            {
                EditorGUILayout.HelpBox("Reset the editor app secret before building.", MessageType.Warning);
                if(GUILayout.Button("Reset"))
                {
                    patcher.EditorAppSecret = Patcher.EditorAllowedSecret;
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }
            }
        }
    }
}