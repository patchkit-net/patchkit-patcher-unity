using System.Net;
using PatchKit.Api;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Licensing
{
    public class KeyLicenseValidator : ILicenseValidator
    {
        private readonly KeysApiConnection _keysApiConnection;

        private readonly string _appSecret;

        public KeyLicenseValidator(string appSecret, KeysApiConnection keysApiConnection)
        {
            _appSecret = appSecret;
            _keysApiConnection = keysApiConnection;
        }

        private string GetAndDeleteCachedKeySecret(string key)
        {
            string keySecret = PlayerPrefs.GetString("PatchKit-" + key + "-KeySecret", null);
            PlayerPrefs.DeleteKey("PatchKit-" + key + "-KeySecret");
            return keySecret;
        }

        private void SaveCachedKeySecret(string key, string keySecret)
        {
            PlayerPrefs.SetString("PatchKit-" + key + "-KeySecret", keySecret);
            PlayerPrefs.Save();
        }

        public string Validate(ILicense license)
        {
            if (license is KeyLicense)
            {
                var keyLicense = (KeyLicense) license;

                try
                {
                    string keySecret = GetAndDeleteCachedKeySecret(keyLicense.Key);

                    var licenseKey = _keysApiConnection.GetKeyInfo(keyLicense.Key, _appSecret, keySecret);

                    SaveCachedKeySecret(keyLicense.Key, licenseKey.KeySecret);

                    return licenseKey.KeySecret;
                }
                catch (WebException webException)
                {
                    if (webException.Response is HttpWebResponse &&
                        (webException.Response as HttpWebResponse).StatusCode == HttpStatusCode.Forbidden)
                    {
                        return null;
                    }
                }
                catch (ApiResponseException apiResponseException)
                {
                    if (apiResponseException.StatusCode == 404)
                    {
                        return null;
                    }
                    throw;
                }
            }

            return null;
        }
    }
}