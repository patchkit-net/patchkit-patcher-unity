﻿using UnityEngine;

namespace PatchKit.Unity.Patcher.UI
{
    public class AnalyticsBanner : MonoBehaviour
    {
        public SettingsList SettingsList;

        void Awake()
        {
            if (PlayerPrefs.GetInt("nextStartPatcher") == 1)
            {
                gameObject.SetActive(false);
            }
        }

        public void SetPermitAnalytics(bool value)
        {
            SettingsList.SetAnalytics = value;
            PlayerPrefs.SetInt("nextStartPatcher", 1);
            PlayerPrefs.Save();
            Patcher.Instance.WaitHandleAnaliticsPopup.Set();
            gameObject.SetActive(false);
        }

        public void OpenWhatDataWebpage()
        {
            Application.OpenURL("https://panel.patchkit.net/");
        }
    }
}