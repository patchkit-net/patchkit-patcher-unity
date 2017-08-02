using JetBrains.Annotations;
using PatchKit.Api;
using UnityEngine;

namespace PatchKit.Unity
{
    public class Settings : ScriptableObject
    {
        private const string AssetFileName = "PatchKit Settings";

        [SerializeField] public ApiConnectionSettings MainApiConnectionSettings;

        [SerializeField] public ApiConnectionSettings KeysApiConnectionSettings;

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
            settings.MainApiConnectionSettings = MainApiConnection.GetDefaultSettings();
            settings.KeysApiConnectionSettings = KeysApiConnection.GetDefaultSettings();

            UnityEditor.AssetDatabase.CreateAsset(settings, string.Format("Assets/PatchKit Patcher/Resources/{0}.asset", AssetFileName));
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

        public static ApiConnectionSettings GetMainApiConnectionSettings()
        {
            var instance = FindInstance();

            return instance == null ? MainApiConnection.GetDefaultSettings() : instance.MainApiConnectionSettings;
        }

        public static ApiConnectionSettings GetKeysApiConnectionSettings()
        {
            var instance = FindInstance();

            return instance == null ? KeysApiConnection.GetDefaultSettings() : instance.KeysApiConnectionSettings;
        }
    }
}