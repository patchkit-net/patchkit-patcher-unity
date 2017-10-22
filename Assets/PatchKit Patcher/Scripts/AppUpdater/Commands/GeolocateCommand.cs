using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using I18N.West;
using Newtonsoft.Json.Linq;
using PatchKit.Api;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class GeolocateCommand : IGeolocateCommand
    {
        private const int Timeout = 10000;
        
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(GeolocateCommand));
        
        public string CountryCode { get; private set; }

        public bool HasCountryCode { get; private set; }

        public void Execute(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Trying to geolocate current host...");
            
            try
            {

                string responseString = null;
                JToken jToken = null;
                
#if UNITY_STANDALONE
                var eventWaitHandle = UnityDispatcher.Invoke(() =>
                {
                    var www = new WWW("https://ip2loc.patchkit.net/v1/country");
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    while (!www.isDone)
                    {
                        if (stopwatch.ElapsedMilliseconds >= Timeout)
                        {
                            break;
                        }
                    }

                    if (!www.isDone)
                    {
                        DebugLogger.LogError("Timeout while getting country code");
                        return;
                    }

                    responseString = www.text;
                    jToken = JToken.Parse(www.text);
                });
                eventWaitHandle.WaitOne();

#else
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;
    
                var apiConnectionSettings = new ApiConnectionSettings
                {
                    CacheServers = new string[0],
                    MainServer = "ip2loc.patchkit.net",
                    Timeout = Timeout,
                    UseHttps = true,
                    Port = 443
                };
                
                var apiConnection = new ApiConnection(apiConnectionSettings);
                DebugLogger.Log("aaa");
                ServicePointManager.ServerCertificateValidationCallback += CertificateValidationCallBack;
                var countryResponse = apiConnection.GetResponse("/v1/country", null);
    
                responseString = countryResponse.Body;
                jToken = countryResponse.GetJson();
#endif
                
                if (jToken != null && jToken.Value<string>("country") != null)
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
                    DebugLogger.LogErrorFormat("Cannot find 'country' key in response json: {0}", responseString);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogErrorFormat("Error while trying to geolocate: " + ex.Message);
                DebugLogger.LogException(ex);
            }
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
            // not needed
        }
    }
}