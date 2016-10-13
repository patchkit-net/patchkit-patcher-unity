using JetBrains.Annotations;
using PatchKit.Api;
using UnityEngine;

namespace PatchKit.Unity
{
    public class Settings : ScriptableObject
    {
        private const string AssetFileName = "PatchKit Settings";

        [SerializeField] public ApiConnectionSettings ApiConnectionSettings;

#if UNITY_EDITOR
        private static Settings CreateSettingsInstance()
        {
            bool pingObject = false;

            if (UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.isPaused = true;

                UnityEditor.EditorUtility.DisplayDialog("PatchKit Settings has been created.", "PatchKit Settings asset has been created.", "OK");

                pingObject = true;
            }

            var settings = CreateInstance<Settings>();
            settings.ApiConnectionSettings = ApiConnectionSettings.CreateDefault();

            UnityEditor.AssetDatabase.CreateAsset(settings, string.Format("Assets/Plugins/PatchKit/Resources/{0}.asset", AssetFileName));
            UnityEditor.EditorUtility.SetDirty(settings);

            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.AssetDatabase.SaveAssets();

            if (pingObject)
            {
                UnityEditor.EditorGUIUtility.PingObject(settings);
            }

            return settings;
        }
#endif

        [CanBeNull]
        public static Settings FindInstance()
        {
            var settings = Resources.Load<Settings>(AssetFileName);

#if UNITY_EDITOR
            if (settings == null)
            {
                settings = CreateSettingsInstance();
            }
#endif
            return settings;
        }

        public static ApiConnectionSettings GetApiConnectionSettings()
        {
            var instance = FindInstance();

            return instance == null ? ApiConnectionSettings.CreateDefault() : instance.ApiConnectionSettings;
        }
    }
}