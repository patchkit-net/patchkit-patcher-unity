using System;
using System.Globalization;
using System.Linq;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine;

namespace PatchKit.Unity.Utilities
{
    public static class CurrentCultureInfo
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CurrentCultureInfo));

        public static CultureInfo GetCurrentCultureInfo()
        {
            try
            {
                SystemLanguage currentLanguage = Application.systemLanguage;
                CultureInfo correspondingCultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures)
                    .FirstOrDefault(x => x.EnglishName.Equals(currentLanguage.ToString()));
                return CultureInfo.CreateSpecificCulture(correspondingCultureInfo.TwoLetterISOLanguageName);
            } catch(Exception e)
            {
                DebugLogger.LogWarning("Unable to get current culture info - " + e);
                return CultureInfo.CurrentCulture;
            }
        }
    }
}
