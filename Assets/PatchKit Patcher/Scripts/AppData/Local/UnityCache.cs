using PatchKit.Logging;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class UnityCache : ICache
    {
        private readonly string _hashedSecret;

        private Logging.ILogger _logger;

        public UnityCache(string appSecret)
        {
            _logger = PatcherLogManager.DefaultLogger;
            _hashedSecret = HashSecret(appSecret);

            _logger.LogDebug("Initializing with: " + _hashedSecret);
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
            UnityDispatcher.Invoke(() => PlayerPrefs.SetString(FormatKey(key), value)).WaitOne();
        }

        public string GetValue(string key, string defaultValue = null)
        {
            string result = string.Empty;
            UnityDispatcher.Invoke(() => result = PlayerPrefs.GetString(FormatKey(key), defaultValue)).WaitOne();
            return result;
        }
    }
}
