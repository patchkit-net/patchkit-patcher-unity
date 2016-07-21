using PatchKit.Api;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PatchKit.Unity.Api
{
    /// <summary>
    /// Settings for <see cref="ApiConnectionInstance"/>.
    /// </summary>
    public class ApiConnectionInstanceSettings : ScriptableObject
    {
        private const string SettingsFileName = "PatchKit API Settings";

        [SerializeField]
        public ApiConnectionSettings ConnectionSettings;

        private static ApiConnectionSettings CreateApiConnectionSettings()
        {
            return new ApiConnectionSettings(10000, "http://api.patchkit.net");
        }

        private static ApiConnectionInstanceSettings FindInstance()
        {
            var settings = Resources.Load<ApiConnectionInstanceSettings>(SettingsFileName);

#if UNITY_EDITOR
            if (settings == null)
            {
                bool pingObject = false;

                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;

                    EditorUtility.DisplayDialog("PatchKit API Settings created!", "PatchKit API Settings has been created.", "OK");

                    pingObject = true;
                }

                settings = CreateInstance<ApiConnectionInstanceSettings>();
                settings.ConnectionSettings = CreateApiConnectionSettings();

                AssetDatabase.CreateAsset(settings, string.Format("Assets/Plugins/PatchKit/Resources/{0}.asset", SettingsFileName));
                EditorUtility.SetDirty(settings);

                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();

                if (pingObject)
                {
                    EditorGUIUtility.PingObject(settings);
                }
            }
#endif
            return settings;
        }

        public static ApiConnectionSettings GetConnectionSettings()
        {
            var instance = FindInstance();

            if (instance == null)
            {
                return CreateApiConnectionSettings();
            }

            return instance.ConnectionSettings;
        }
    }
}
