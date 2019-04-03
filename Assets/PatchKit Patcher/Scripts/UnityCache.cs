using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class UnityCache
    {
        private readonly string _hashedSecret;

        public UnityCache(string appSecret)
        {
            _hashedSecret = HashSecret(appSecret);
        }

        private string HashSecret(string secret)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(secret);
                return string.Join("", md5.ComputeHash(bytes).Select(b => b.ToString("x2")).ToArray());
            }
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
