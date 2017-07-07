using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    class UnityCache : ICache
    {
        public void SetValue(string key, string value)
        {
            UnityDispatcher.Invoke(() => PlayerPrefs.SetString(key, value)).WaitOne();
        }

        public string GetValue(string key, string defaultValue = null)
        {
            string result = string.Empty;
            UnityDispatcher.Invoke(() => result = PlayerPrefs.GetString(key, defaultValue)).WaitOne();
            return result;
        }
    }
}
