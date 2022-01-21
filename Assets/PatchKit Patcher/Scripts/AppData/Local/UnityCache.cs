using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class UnityCache : ICache
    {
        private readonly string _hashedSecret;

        public UnityCache(string appSecret)
        {
            _hashedSecret = HashSecret(appSecret);
        }

        private string HashSecret(string secret)
        {
            return HashCalculator.ComputeMD5Hash(secret);
        }

        private string FormatKey(string key)
        {
            return _hashedSecret + "-" + key;
        }

        public void SetValue(string key, string value)
        {
            UnityDispatcher.Invoke(() =>
            {
                PlayerPrefs.SetString(FormatKey(key), value);
                PlayerPrefs.Save();
            }).WaitOne();
        }

        public string GetValue(string key, string defaultValue = null)
        {
            string result = string.Empty;
            UnityDispatcher.Invoke(() => result = PlayerPrefs.GetString(FormatKey(key), defaultValue)).WaitOne();
            return result;
        }

        public void SetInt(string key, int value)
        {
            UnityDispatcher.Invoke(() =>
            {
                PlayerPrefs.SetInt(FormatKey(key), value);
                PlayerPrefs.Save();
            }).WaitOne();
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            int result = 0;
            UnityDispatcher.Invoke(() => result = PlayerPrefs.GetInt(FormatKey(key), defaultValue)).WaitOne();
            return result;
        }
    }
}