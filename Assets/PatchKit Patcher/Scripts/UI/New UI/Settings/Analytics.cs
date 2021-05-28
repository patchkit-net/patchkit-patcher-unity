using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.UI.Dialogs;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class Analytics : Dialog<Analytics>
    {
        public SettingsList SettingsList;

        public void SetPermitAnalytics(bool value)
        {
            SettingsList.SetAnalytics = value;
            Patcher.Instance.WaitHandleAnaliticsPopup.Set();
            OnDisplayed();
        }

        public void OpenWhatDataWebpage()
        {
            Application.OpenURL("https://panel.patchkit.net/");
        }

        public void Display()
        {
            base.Display(CancellationToken.Empty);
        }
    }
}
