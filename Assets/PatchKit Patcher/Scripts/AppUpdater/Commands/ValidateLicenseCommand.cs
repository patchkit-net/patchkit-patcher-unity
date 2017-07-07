using System;
using System.Net;
using PatchKit.Api;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Patcher.UI.Dialogs;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class ValidateLicenseCommand : BaseAppUpdaterCommand, IValidateLicenseCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(ValidateLicenseCommand));

        private readonly ILicenseDialog _licenseDialog;
        private readonly IRemoteMetaData _remoteMetaData;
        private readonly ICache _cache;

        public ValidateLicenseCommand(ILicenseDialog licenseDialog, IRemoteMetaData remoteMetaData, ICache cache)
        {
            Checks.ArgumentNotNull(licenseDialog, "licenseDialog");
            Checks.ArgumentNotNull(remoteMetaData, "remoteMetaData");
            Checks.ArgumentNotNull(cache, "cache");

            DebugLogger.LogConstructor();

            _licenseDialog = licenseDialog;
            _remoteMetaData = remoteMetaData;
            _cache = cache;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Validating license.");

            KeySecret = null;

            var appInfo = _remoteMetaData.GetAppInfo();

            if (!appInfo.UseKeys)
            {
                DebugLogger.Log("Application is not using license keys.");
                return;
            }

            LicenseDialogMessageType messageType = LicenseDialogMessageType.None;

            var cachedKey = GetCachedKey();
            bool triedCachedKey = false;

            while (KeySecret == null)
            {
                string key = string.Empty;

                if (!triedCachedKey && !string.IsNullOrEmpty(cachedKey))
                {
                    DebugLogger.Log("Using cached license key.");

                    key = cachedKey;
                    triedCachedKey = true;
                }
                else
                {
                    DebugLogger.Log("Displaying license dialog.");

                    var result = _licenseDialog.Display(messageType);

                    DebugLogger.Log("Processing dialog result.");

                    if (result.Type == LicenseDialogResultType.Confirmed)
                    {
                        DebugLogger.Log("Using license key typed in dialog.");
                        key = result.Key;
                    }
                    else if (result.Type == LicenseDialogResultType.Aborted)
                    {
                        DebugLogger.Log("License dialog has been aborted. Cancelling operation.");
                        throw new OperationCanceledException();
                    }
                }

                try
                {
                    KeySecret = _remoteMetaData.GetKeySecret(key, GetCachedKeySecret(key));

                    DebugLogger.LogVariable(KeySecret, "KeySecret");
                    DebugLogger.Log("License key has been validated");

                    SetCachedKey(key);
                    SetCachedKeySecret(key, KeySecret);
                }
                catch (ApiResponseException apiResponseException)
                {
                    DebugLogger.LogException(apiResponseException);

                    if (apiResponseException.StatusCode == 404)
                    {
                        KeySecret = null;
                        messageType = LicenseDialogMessageType.InvalidLicense;
                    }
                    else if (apiResponseException.StatusCode == 410)
                    {
                        KeySecret = null;
                        messageType = LicenseDialogMessageType.BlockedLicense;
                    }
                    else if (apiResponseException.StatusCode == 403)
                    {
                        KeySecret = null;
                        messageType = LicenseDialogMessageType.ServiceUnavailable;
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (WebException webException)
                {
                    DebugLogger.LogException(webException);

                    KeySecret = null;
                    messageType = LicenseDialogMessageType.ServiceUnavailable;

                    if (webException.Status == WebExceptionStatus.ProtocolError)
                    {
                        var response = (HttpWebResponse) webException.Response;
                        if ((int)response.StatusCode == 404)
                        {
                            messageType = LicenseDialogMessageType.InvalidLicense;
                        }
                        else if ((int)response.StatusCode == 410)
                        {
                            messageType = LicenseDialogMessageType.BlockedLicense;
                        }
                        else if ((int)response.StatusCode == 403)
                        {
                            messageType = LicenseDialogMessageType.ServiceUnavailable;
                        }
                    }
                }
            }
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            Checks.ArgumentNotNull(statusMonitor, "statusMonitor");
        }

        public string KeySecret { get; private set; }

        private void SetCachedKey(string value)
        {
            _cache.SetValue("patchkit-key", value);
        }

        private string GetCachedKey()
        {
            return _cache.GetValue("patchkit-key");
        }

        private void SetCachedKeySecret(string key, string value)
        {
            _cache.SetValue(string.Format("patchkit-keysecret-{0}", key), value);
        }

        private string GetCachedKeySecret(string key)
        {
            return _cache.GetValue(string.Format("patchkit-keysecret-{0}", key));
        }
    }
}