using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.UI.Languages
{
    public static class PatcherLanguages
    {
        private static Dictionary<String, String> Fields;
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(PatcherLanguages));
        private const string CachePatchKitLanguages = "patchkit-language";

        public const string OpenTag = "<key>";
        public const string CloseTag = "</key>";
        public static string language;
        public static string DefaultLanguage = "en";

        public static void SetLanguage(string newlanguage)
        {
            language = newlanguage;
            PlayerPrefs.SetString(CachePatchKitLanguages, language);
            PlayerPrefs.Save();
            ChangeLaguage();
        }
        
        private static void LoadLanguage()
        {
            language = PlayerPrefs.GetString(CachePatchKitLanguages);
            if (String.IsNullOrEmpty(language))
            {
                CultureInfo cultureInfo = CurrentCultureInfo.GetCurrentCultureInfo();
                language = cultureInfo.TwoLetterISOLanguageName;
                language = EnvironmentInfo.GetEnvironmentVariable(
                    EnvironmentVariables.TranslationLanguageEnvironmentVariable, language);
            }

            ChangeLaguage();
            try
            {
                DropdownLanguages.SetValue(language);
            }
            catch (Exception e)
            {
                language = DefaultLanguage;
                ChangeLaguage();
                DebugLogger.LogWarning("Not found DropdownLanguages");
            }
        }

        private static void ChangeLaguage()
        {
            if (Fields == null)
                Fields = new Dictionary<string, string>();
            Fields.Clear();
            string path;
            
            try
            {
                path = ((TextAsset) Resources.Load(@"Languages/" + language)).text; //without (.json)
            }
            catch
            {
                language = DefaultLanguage;
                path = ((TextAsset) Resources.Load(@"Languages/" + language)).text; //without (.json)
                DebugLogger.LogWarning(String.Format("Unable to find {0} language file. Default language is set.",
                    language));
            }

            if (!String.IsNullOrEmpty(path))
            {
                Fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(path);
            }
        }

        public static string GetTranslation(string key)
        {
            if (Fields == null)
                LoadLanguage();
            if (!String.IsNullOrEmpty(key))
            {
                if (!Fields.ContainsKey(key))
                {
                    DebugLogger.LogError(String.Format("There is no key with name: [{0}] in your language file", key));
                    return "";
                }
            }
            else
            {
                return "";
            }

            return Fields[key];
        }
        
        public static string GetTranslationText(string text, params object[] args)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            while(text.Contains(OpenTag) && text.Contains(CloseTag))
            {
                int Start, End;
                Start = text.IndexOf(OpenTag, 0) + OpenTag.Length;
                End = text.IndexOf(CloseTag, Start);
                string key = text.Substring(Start, End - Start);
                text = string.Format(text.Replace(OpenTag + key + CloseTag, GetTranslation(key)), args);
            }
            return text;
        }
    }
}
