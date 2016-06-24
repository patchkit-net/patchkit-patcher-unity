using PatchKit.API;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace PatchKit.Unity.API
{
    /// <summary>
    /// PatchKit Unity API settings.
    /// </summary>
    public class PatchKitUnityAPISettings : ScriptableObject
    {
        private const string SettingsFileName = "PatchKit API Settings";

        [Header("The url of main API server.")]
        public string Url = "http://api.patchkit.net";

        [Header("Urls of mirror API servers.")]
        public string[] MirrorUrls;

        [Header("Delay between which mirror requests are sent (ms)."), Range(1.0f, 60000.0f)]
        public long DelayBetweenMirrorRequests = 500;

        private static PatchKitUnityAPISettings FindInstance()
        {
            var settings = Resources.Load<PatchKitUnityAPISettings>(SettingsFileName);

#if UNITY_EDITOR
            if (settings == null)
            {
                bool pingObject = false;

                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;

                    EditorUtility.DisplayDialog("PatchKit API Settings created!", "PatchKit Settings has been created.", "OK");

                    pingObject = true;
                }

                settings = CreateInstance<PatchKitUnityAPISettings>();

                AssetDatabase.CreateAsset(settings, string.Format("Assets/Plugins/PatchKit/Resources/{0}.assets", SettingsFileName));
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


        public static PatchKitAPISettings GetSettings()
        {
            var instance = FindInstance();

            if (instance == null)
            {
                return new PatchKitAPISettings();
            }

            return new PatchKitAPISettings()
            {
                DelayBetweenMirrorRequests = instance.DelayBetweenMirrorRequests,
                MirrorUrls = instance.MirrorUrls,
                Url = instance.Url
            };
        }
    }
}
