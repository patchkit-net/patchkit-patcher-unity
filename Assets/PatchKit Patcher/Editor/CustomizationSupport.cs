using System.Collections.Generic;
using PatchKit.Unity.Patcher.UI;
using PatchKit.Unity.Patcher.UI.Dialogs;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PatchKit.Unity.Editor
{
    [InitializeOnLoad]
    public class CustomizationSupport : UnityEditor.Editor
    {
        private static readonly float _btnHeight = 20;
        private static readonly float _btnPadding = 5;
        private static float _btnY;
        private static Scene _currentScene;
        private static List<ComponentPatcher> _cacheComponentsPatcher = new List<ComponentPatcher>();
        private static ComponentPatcher _logoPatchkit;

        public class ComponentPatcher
        {
            public GameObject GameObject;
            public string Name;

            public bool Active
            {
                get { return GameObject.activeSelf; }
                set { GameObject.SetActive(value); }
            }

            public ComponentPatcher(GameObject gameObject, string name)
            {
                GameObject = gameObject;
                Name = name;
            }
        }

        static CustomizationSupport()
        {
            SceneView.onSceneGUIDelegate += OnScene;
        }

        private static void TryCacheComponentPatcher<T>(string name) where T : MonoBehaviour
        {
            GameObject gameObject;
            if (TryGetGameObject<T>(out gameObject))
                _cacheComponentsPatcher.Add(new ComponentPatcher(gameObject, name));
        }

        private static bool TryGetGameObject<T>(out GameObject gameObject) where T : MonoBehaviour
        {
            var component = Resources.FindObjectsOfTypeAll<T>();
            if (component.Length == 1)
            {
                gameObject = component[0].gameObject;
                return true;
            }

            gameObject = null;
            return false;
        }

        private static void OnScene(SceneView sceneview)
        {
            if (_currentScene != SceneManager.GetActiveScene())
            {
                _cacheComponentsPatcher.Clear();
                _logoPatchkit = null;

                TryCacheComponentPatcher<MessagePanel>("Message Panel");
                TryCacheComponentPatcher<LicenseDialog>("License Dialog");
                TryCacheComponentPatcher<AnalyticsBanner>("Analytics Banner");
                TryCacheComponentPatcher<AnalyticsPopup>("Analytics Popup");
                TryCacheComponentPatcher<ErrorDialog>("Error Panel");
                GameObject gameObject;
                if (TryGetGameObject<PatchKitLogo>(out gameObject))
                {
                    _logoPatchkit = new ComponentPatcher(gameObject, "PatchKit Logo");
                }

                _currentScene = SceneManager.GetActiveScene();
            }
            else
            {
                _btnY = _btnPadding;
                Handles.BeginGUI();
                if (_logoPatchkit != null)
                    AddButton(_logoPatchkit);
                foreach (var componentPatcher in _cacheComponentsPatcher)
                {
                    AddButton(componentPatcher);
                }

                Handles.EndGUI();
            }
        }

        static Rect GetRect(float y)
        {
            return new Rect(_btnPadding, y, 110, _btnHeight);
        }

        static void AddButton(ComponentPatcher componentPatcher)
        {
            if (!componentPatcher.Active)
                GUI.color = Color.grey;
            else
                GUI.color = Color.white;

            if (GUI.Button(GetRect(_btnY), componentPatcher.Name))
            {
                GameObject gameObject = componentPatcher.GameObject;
                gameObject.SetActive(!gameObject.activeSelf);
            }

            _btnY += (_btnHeight + _btnPadding);
        }

        public static void SetActivePatcherComponentsAll(bool active)
        {
            foreach (var componentPatcher in _cacheComponentsPatcher)
            {
                componentPatcher.Active = active;
            }
        }

        public static bool TryGetPatchKitLogo(out PatchKitLogo patchKitLogo)
        {
            var component = FindObjectOfType<PatchKitLogo>();
            if (component != null)
            {
                patchKitLogo = component;
                return true;
            }

            patchKitLogo = null;
            return false;
        }
    }
}