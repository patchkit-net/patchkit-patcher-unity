using PatchKit.Patching.AppData.Local;
using UnityEngine;

namespace PatchKit.Patching.Unity
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

        public void DeleteKey(string key)
        {
            UnityDispatcher.Invoke(() => PlayerPrefs.DeleteKey(key)).WaitOne();
        }

        public bool HasKey(string key)
        {
            bool result = default(bool);
            UnityDispatcher.Invoke(() => result = PlayerPrefs.HasKey(key)).WaitOne();
            return result;
        }
    }
}
