using System.Net;
using PatchKit.Api;

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

        public string Validate(ILicense license)
        {
            if (license is KeyLicense)
            {
                var keyLicense = (KeyLicense) license;

                try
                {
                    var licenseKey = _keysApiConnection.GetKeyInfo(keyLicense.Key, _appSecret);

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