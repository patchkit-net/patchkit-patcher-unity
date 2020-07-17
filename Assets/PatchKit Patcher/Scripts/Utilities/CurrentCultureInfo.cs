using System.Globalization;
using System.Linq;
using UnityEngine;

namespace PatchKit.Unity.Utilities
{
    public static class CurrentCultureInfo
    {
        public static CultureInfo GetCurrentCultureInfo()
        {
            SystemLanguage currentLanguage = Application.systemLanguage;
            CultureInfo correspondingCultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .FirstOrDefault(x => x.EnglishName.Equals(currentLanguage.ToString()));
            return CultureInfo.CreateSpecificCulture(correspondingCultureInfo.TwoLetterISOLanguageName);
        }
    }
}
