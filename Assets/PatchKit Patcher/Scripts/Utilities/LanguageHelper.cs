using PatchKit.Unity.UI.Languages;

namespace PatchKit.Unity.Utilities
{
    public static class LanguageHelper
    {
        public static string Tag(string key)
        {
            return PatcherLanguages.OpenTag + key + PatcherLanguages.CloseTag;
        }
    }
}