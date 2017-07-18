using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json.Linq;
using PatchKit.Api;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class GeolocateCommand : IGeolocateCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(GeolocateCommand));
        
        public string CountryCode { get; private set; }

        public bool HasCountryCode { get; private set; }

        public void Execute(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Trying to geolocate current host...");
            try
            {
                var apiConnectionSettings = new ApiConnectionSettings
                {
                    CacheServers = new string[0],
                    MainServer = "ip2loc.patchkit.net",
                    Timeout = 10000,
                    UseHttps = true,
                    Port = 443
                };
                
                var apiConnection = new ApiConnection(apiConnectionSettings);
                DebugLogger.Log("aaa");
                ServicePointManager.ServerCertificateValidationCallback += CertificateValidationCallBack;
                var countryResponse = apiConnection.GetResponse("/v1/country", null);
                JToken jToken = countryResponse.GetJson();
                
                if (jToken.Contains("country"))
                {
                    CountryCode = jToken.Value<string>("country");
                    HasCountryCode = !string.IsNullOrEmpty(CountryCode);

                    if (HasCountryCode)
                    {
                        DebugLogger.LogFormat("Geolocation succeeded! Country code: '{0}'", CountryCode);
                    }
                    else
                    {
                        DebugLogger.LogWarning("Geolocation succeeded, but empty country code received.");
                    }
                }
                else
                {
                    DebugLogger.LogErrorFormat("Cannot find 'country' key in response json: {0}", countryResponse.Body);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogErrorFormat("Error while trying to geolocate: " + ex.Message);
                DebugLogger.LogException(ex);
            }
        }

        private bool CertificateValidationCallBack(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            DebugLogger.Log("HERE");
            // If the certificate is a valid, signed certificate, return true.
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }

            // If there are errors in the certificate chain, look at each error to determine the cause.
            if ((sslPolicyErrors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                if (chain != null && chain.ChainStatus != null)
                {
                    foreach (System.Security.Cryptography.X509Certificates.X509ChainStatus status in chain.ChainStatus)
                    {
                        if ((certificate.Subject == certificate.Issuer) &&
                            (status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags
                                 .UntrustedRoot))
                        {
                            // Self-signed certificates with an untrusted root are valid. 
                            continue;
                        }
                        else
                        {
                            if (status.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags
                                    .NoError)
                            {
                                // If there are any other errors in the certificate chain, the certificate is invalid,
                                // so the method returns false.
                                return false;
                            }
                        }
                    }
                }

                // When processing reaches this line, the only errors in the certificate chain are 
                // untrusted root errors for self-signed certificates. These certificates are valid
                // for default Exchange server installations, so return true.
                return true;
            }
            else
            {
                // In all other cases, return false.
                return false;
            }
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
            // not needed
        }
    }
}