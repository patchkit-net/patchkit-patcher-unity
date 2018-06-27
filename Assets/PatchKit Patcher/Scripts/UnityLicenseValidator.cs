using System;
using System.Threading;
using JetBrains.Annotations;
using PatchKit.Api;
using PatchKit.Apps;
using PatchKit.Apps.Updating;
using PatchKit.Apps.Updating.Licensing;
using PatchKit.Logging;
using PatchKit.Patching.Unity.UI.Dialogs;

namespace PatchKit.Patching.Unity
{
    public class UnityLicenseValidator
    {
        [NotNull] private readonly ILicenseDialog _licenseDialog;
        [NotNull] private readonly ILogger _logger;
        [NotNull] private readonly IKeysAppLicenseAuthorizer _keysAppLicenseAuthorizer;
        [NotNull] private readonly IDataClient _dataClient;

        public UnityLicenseValidator(string path, [NotNull] ILicenseDialog licenseDialog)
        {
            if (licenseDialog == null)
            {
                throw new ArgumentNullException(nameof(licenseDialog));
            }

            _licenseDialog = licenseDialog;
            _logger = DependencyResolver.Resolve<ILogger>();
            var metaDataClient = DependencyResolver.Resolve<MetaDataClientFactory>()(path);
            _dataClient = DependencyResolver.Resolve<DataClientFactory>()(path, metaDataClient);
            _keysAppLicenseAuthorizer = DependencyResolver.Resolve<IKeysAppLicenseAuthorizer>();
        }

        public void Validate(string appSecret, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Validating license...");

                var messageType = LicenseDialogMessageType.None;

                var cachedKey = GetCachedKey();
                _logger.LogTrace("Cached key = " + cachedKey);

                bool didUseCachedKey = false;

                while (!AppLicense.HasValue)
                {
                    bool isUsingCachedKey;
                    string key = GetKey(messageType, cachedKey, out isUsingCachedKey, ref didUseCachedKey);

                    try
                    {
                        _logger.LogTrace("Key = " + key);

                        _logger.LogDebug("Validating key...");

                        AppLicense = _keysAppLicenseAuthorizer.Authorize(appSecret, key, cancellationToken);

                        _logger.LogDebug("License has been validated!");

                        _logger.LogTrace("KeySecret = " + AppLicense.Value.Secret);

                        _logger.LogDebug("Saving key and key secret to cache.");
                        SetCachedKey(key);
                    }
                    catch (InvalidLicenseException invalidLicenseException)
                    {
                        _logger.LogWarning(
                            "Key validation failed due to invalid license. Setting license dialog message to InvalidLicense",
                            invalidLicenseException);

                        HandleApiError(ref messageType, isUsingCachedKey, LicenseDialogMessageType.InvalidLicense);
                    }
                    catch (BlockedLicenseException blockedLicenseException)
                    {
                        _logger.LogWarning(
                            "Key validation failed due to blocked license. Setting license dialog message to BlockedLicense",
                            blockedLicenseException);

                        HandleApiError(ref messageType, isUsingCachedKey, LicenseDialogMessageType.BlockedLicense);
                    }
                    catch (ApiResponseException apiResponseException)
                    {
                        _logger.LogWarning(
                            "Key validation failed due to connection issues with API server. Setting license dialog message to ServiceUnavailable",
                            apiResponseException);
                        messageType = LicenseDialogMessageType.ServiceUnavailable;
                    }
                    catch (ApiConnectionException apiConnectionException)
                    {
                        _logger.LogWarning(
                            "Key validation failed due to connection issues with API server. Setting license dialog message to ServiceUnavailable",
                            apiConnectionException);
                        messageType = LicenseDialogMessageType.ServiceUnavailable;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Validating license has failed.", e);
                throw;
            }
        }

        private string GetKey(LicenseDialogMessageType messageType, string cachedKey, out bool isUsingCachedKey,
            ref bool didUseCachedKey)
        {
            bool isCachedKeyAvailable = !string.IsNullOrEmpty(cachedKey);

            if (isCachedKeyAvailable && !didUseCachedKey)
            {
                _licenseDialog.SetKey(cachedKey);
                didUseCachedKey = true;
                isUsingCachedKey = true;

                return cachedKey;
            }

            isUsingCachedKey = false;

            return GetKeyFromDialog(messageType);
        }

        private string GetKeyFromDialog(LicenseDialogMessageType messageType)
        {
            _logger.LogDebug("Displaying license dialog...");

            var result = _licenseDialog.Display(messageType);

            _logger.LogDebug("License dialog has returned result.");

            _logger.LogTrace("result.Key = " + result.Key);
            _logger.LogTrace($"result.Type = {result.Type}");

            switch (result.Type)
            {
                case LicenseDialogResultType.Confirmed:
                    _logger.LogDebug("Using key typed in license dialog.");
                    return result.Key;
                case LicenseDialogResultType.Aborted:
                    _logger.LogDebug("License dialog has been aborted. Cancelling operation.");
                    throw new OperationCanceledException();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleApiError(ref LicenseDialogMessageType messageType, bool isUsingCachedKey,
            LicenseDialogMessageType licenseDialogMessageType)
        {
            if (!isUsingCachedKey)
            {
                _logger.LogDebug($"Setting license dialog message to {licenseDialogMessageType}");
                messageType = licenseDialogMessageType;
            }
            else
            {
                _logger.LogDebug(
                    "Ignoring API error - the attempt was done with cached key. Prompting user to enter new license key.");
            }
        }

        public AppLicense? AppLicense;

        private void SetCachedKey(string value)
        {
            var appInfo = _dataClient.GetAppInfo();
            _dataClient.SetAppInfo(new AppInfo(appInfo.Secret, value));
        }

        private string GetCachedKey()
        {
            var lastUsedLicenseKey = _dataClient.GetAppInfo().LastUsedLicenseKey;
            return lastUsedLicenseKey?.Value;
        }
    }
}