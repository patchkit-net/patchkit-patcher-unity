using UnityEngine;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    class UnityCache : ICache
    {
        public void SetValue(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public string GetValue(string key, string defaultValue = null)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }
    }
}
