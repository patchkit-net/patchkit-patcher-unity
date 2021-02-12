using System;
using System.Collections.Generic;
using PatchKit.Unity.Patcher.UI;
using PatchKit.Unity.Patcher.UI.Dialogs;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace PatchKit.Unity.Editor
{
    [InitializeOnLoad]
    public class CustomizationSupport : UnityEditor.Editor
    {
        static float btnHeight = 20;
        static float btnPadding = 5;
        static float btnY;
        private static List<ComponentPatcher> _cacheComponentsPatcher = new List<ComponentPatcher>();
        public static GUIStyle style;

        class ComponentPatcher
        {
            public GameObject GameObject;
            public string Name;
            public bool Active
            {
                get { return GameObject.activeSelf;}
            }
            public ComponentPatcher(GameObject gameObject, string name)
            {
                GameObject = gameObject;
                Name = name;
            }
        }

        static CustomizationSupport()
        {
            _cacheComponentsPatcher.Add(new ComponentPatcher(GetGameObject<MessagePanel>(), "Message Panel"));
            _cacheComponentsPatcher.Add(new ComponentPatcher(GetGameObject<LicenseDialog>(), "License Dialog"));
            _cacheComponentsPatcher.Add(new ComponentPatcher(GetGameObject<AnalyticsBanner>(), "Analytics Banner"));
            _cacheComponentsPatcher.Add(new ComponentPatcher(GetGameObject<AnalyticsPopup>(), "Analytics Popup"));
            _cacheComponentsPatcher.Add(new ComponentPatcher(GetGameObject<ErrorDialog>(), "Error Panel"));
            SceneView.onSceneGUIDelegate += OnScene;
        }
        
        private static void OnScene(SceneView sceneview)
        {
            btnY = btnPadding;
            Handles.BeginGUI();
            foreach (var componentPatcher in _cacheComponentsPatcher)
            {
                AddButton(componentPatcher);
            }

            Handles.EndGUI();
        }

        static Rect GetRect(float y)
        {
            return new Rect(btnPadding, y, 110, btnHeight);
        }

        static void AddButton(ComponentPatcher componentPatcher)
        {
            if(!componentPatcher.Active)
                GUI.color = Color.grey;
            else
                GUI.color = Color.white;

            if (GUI.Button(GetRect(btnY), componentPatcher.Name))
            {
                GameObject gameObject = componentPatcher.GameObject;
                gameObject.SetActive(!gameObject.activeSelf);
            }
            btnY += (btnHeight + btnPadding);
        }

        private static GameObject GetGameObject<T>() where T : MonoBehaviour
        {
            return Resources.FindObjectsOfTypeAll<T>()[0].gameObject;
        }
    }
}