using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.UI.Languages
{
    public class PatcherLanguages : MonoBehaviour
    {
        public static PatcherLanguages Instance;

        public static Dictionary<String, String> Fields;

        [SerializeField] 
        string defaultLanguage = "en";

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(PatcherLanguages));

        void Awake ()
        {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad (gameObject);
            } else {
                Destroy (gameObject);
            }

            LoadLanguage ();
        }


        void LoadLanguage ()
        {
            if (Fields == null)
                Fields = new Dictionary<string, string> ();
        
            Fields.Clear ();
            string allTexts;
            CultureInfo cultureInfo = CurrentCultureInfo.GetCurrentCultureInfo();
            string language = cultureInfo.TwoLetterISOLanguageName;
            language = EnvironmentInfo.GetEnvironmentVariable(EnvironmentVariables.TranslationLanguageEnvironmentVariable, language);

            try
            {
                allTexts = (Resources.Load(@"Languages/" + language) as TextAsset).text; //without (.json)
            }
            catch
            {
                allTexts = (Resources.Load(@"Languages/" + defaultLanguage) as TextAsset).text; //without (.json)
                DebugLogger.LogWarning(String.Format("Unable to find {0} language file. Default language is set.", language));
            }

            if (!String.IsNullOrEmpty(allTexts))
            {
                Fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(allTexts);
            }
        }

        public static string GetTraduction (string key)
        {
            if (!String.IsNullOrEmpty(key))
            {
                if (!Fields.ContainsKey(key))
                {
                    DebugLogger.LogError(String.Format("There is no key with name: [{0}] in your language file", key));
                    return null;
                }
            }
            else
            {
                return null;
            }

            return Fields [key];
        }
    }
}
