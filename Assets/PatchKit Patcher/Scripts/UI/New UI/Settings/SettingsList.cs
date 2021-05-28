using System;
using System.Collections;
using System.Collections.Generic;
using PatchKit.Unity.Patcher;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class SettingsList : MonoBehaviour
    {
        public Toggle ToggleAnalytics;

        private void Awake()
        {
            if (PlayerPrefs.GetInt("analytics") == 1)
            {
                SetAnalytics = true;
            }
        }

        public bool SetAnalytics
        {
            set
            {
                PatcherStatistics.SetPermitStatistics(value);
                ToggleAnalytics.isOn = value;
                PlayerPrefs.SetInt("analytics", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
