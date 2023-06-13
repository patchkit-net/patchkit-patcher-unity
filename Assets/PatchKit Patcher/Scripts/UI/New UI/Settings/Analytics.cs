using PatchKit.Unity.Patcher.UI.Dialogs;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class Analytics<T> : Dialog<T> where T : Analytics<T>
    {
        public SettingsList SettingsList;

        public void SetPermitAnalytics(bool value)
        {
            SettingsList.SetAnalytics = value;
            OnDisplayed();
        }

        public void OpenWhatDataWebpage()
        {
            Application.OpenURL("https://patchkit.net/privacy_policy.html");
        }

        public virtual void Display() { }
    }
}
