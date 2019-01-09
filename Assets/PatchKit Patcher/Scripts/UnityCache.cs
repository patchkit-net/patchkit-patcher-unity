using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PatchKit.Patching.Unity
{
    class UnityCache
    {
        private string EncodeSecret(string secret)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(secret);
                return string.Join("", md5.ComputeHash(bytes).Select(b => b.ToString("x2")).ToArray());
            }
        }
        
        private string FormatKey(string key, string secret)
        {
            return EncodeSecret(secret) + key;
        }
        
        private string FormatKey(string key)
        {
            if (!Patcher.Instance.Data.HasValue)
            {
                throw new ApplicationException("Cannot cache without application secret.");
            }
            
            return FormatKey(key, Patcher.Instance.Data.Value.AppSecret);
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

        public void DeleteKey(string key)
        {
            UnityDispatcher.Invoke(() => PlayerPrefs.DeleteKey(FormatKey(key))).WaitOne();
        }

        public bool HasKey(string key)
        {
            bool result = default(bool);
            UnityDispatcher.Invoke(() => result = PlayerPrefs.HasKey(FormatKey(key))).WaitOne();
            return result;
        }
    }
}
