using System;
using JetBrains.Annotations;
using PatchKit.Api;
using PatchKit.IssueReporting;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.UI.Dialogs;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class ValidateLicenseCommand : BaseAppUpdaterCommand, IValidateLicenseCommand
    {
        private const string CachePatchKitKeySecret = "patchkit-keysecret-";

        [NotNull] private readonly ILicenseDialog _licenseDialog;
        [NotNull] private readonly IRemoteMetaData _remoteMetaData;
        [NotNull] private readonly ILocalMetaData _localMetaData;
        [NotNull] private readonly ICache _cache;
        [NotNull] private readonly ILogger _logger;

        public ValidateLicenseCommand([NotNull] ILicenseDialog licenseDialog, [NotNull] IRemoteMetaData remoteMetaData,
            [NotNull] ILocalMetaData localMetaData, [NotNull] ICache cache, [NotNull] ILogger logger, [NotNull] IIssueReporter issueReporter)
        {
            if (licenseDialog == null) throw new ArgumentNullException("licenseDialog");
            if (remoteMetaData == null) throw new ArgumentNullException("remoteMetaData");
            if (localMetaData == null) throw new ArgumentNullException("localMetaData");
            if (cache == null) throw new ArgumentNullException("cache");
            if (logger == null) throw new ArgumentNullException("logger");
            if (issueReporter == null) throw new ArgumentNullException("issueReporter");

            _licenseDialog = licenseDialog;
            _remoteMetaData = remoteMetaData;
            _localMetaData = localMetaData;
            _cache = cache;
            _logger = logger;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            try
            {
                PatcherStatistics.TryDispatchSendEvent(PatcherStatistics.Event.LicenseKeyVerificationStarted);

                _logger.LogDebug("Validating license...");

                base.Execute(cancellationToken);

                KeySecret = null;

                var appInfo = _remoteMetaData.GetAppInfo(true, cancellationToken);

                if (!appInfo.UseKeys)
                {
                    _logger.LogDebug("Validating license is not required - application is not using license keys.");
                    return;
                }

                var messageType = LicenseDialogMessageType.None;

                var cachedKey = GetCachedKey();
                _logger.LogTrace("Cached key = " + cachedKey);

                bool didUseCachedKey = false;

                while (KeySecret == null)
                {
                    bool isUsingCachedKey;
                    string key = GetKey(messageType, cachedKey, out isUsingCachedKey, ref didUseCachedKey);

                    try
                    {
                        _logger.LogTrace("Key = " + key);

                        var cachedKeySecret = GetCachedKeySecret(key);
                        _logger.LogTrace("Cached key secret = " + cachedKeySecret);

                        _logger.LogDebug("Validating key...");

                        KeySecret = _remoteMetaData.GetKeySecret(key, cachedKeySecret, cancellationToken);

                        _logger.LogDebug("License has been validated!");
                        PatcherStatistics.TryDispatchSendEvent(PatcherStatistics.Event.LicenseKeyVerificationSucceeded);

                        _logger.LogTrace("KeySecret = " + KeySecret);

                        _logger.LogDebug("Saving key and key secret to cache.");
                        SetCachedKey(key);
                        SetCachedKeySecret(key, KeySecret);
                    }
                    catch (ApiResponseException apiResponseException)
                    {
                        _logger.LogWarning(
                            "Key validation failed due to server or API error. Checking if error can be recognized and displayed to user...",
                            apiResponseException);

                        if (TryToHandleApiErrors(apiResponseException.StatusCode, ref messageType, isUsingCachedKey))
                        {
                            PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.LicenseKeyVerificationFailed);
                        }
                        else
                        {
                            throw;
                        }
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
            _logger.LogTrace(string.Format("result.Type = {0}", result.Type));

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

        private bool TryToHandleApiErrors(int statusCode, ref LicenseDialogMessageType messageType,
            bool isUsingCachedKey)
        {
            _logger.LogTrace(string.Format("isUsingCachedKey = {0}", isUsingCachedKey));
            _logger.LogTrace(string.Format("statusCode = {0}", statusCode));

            if (statusCode == 404)
            {
                _logger.LogDebug("License key is not found.");
                HandleApiError(ref messageType, isUsingCachedKey, LicenseDialogMessageType.InvalidLicense);
                return true;
            }
            if (statusCode == 410)
            {
                _logger.LogDebug("License key is blocked.");
                HandleApiError(ref messageType, isUsingCachedKey, LicenseDialogMessageType.BlockedLicense);
                return true;
            }
            if (statusCode == 403)
            {
                _logger.LogDebug(
                    "License key validation service is not available.");
                HandleApiError(ref messageType, isUsingCachedKey, LicenseDialogMessageType.ServiceUnavailable);
                return true;
            }

            _logger.LogError("Unrecognized server or API error.");
            return false;
        }

        private void HandleApiError(ref LicenseDialogMessageType messageType, bool isUsingCachedKey,
            LicenseDialogMessageType licenseDialogMessageType)
        {
            if (!isUsingCachedKey)
            {
                _logger.LogDebug(string.Format("Setting license dialog message to {0}", licenseDialogMessageType));
                messageType = licenseDialogMessageType;
            }
            else
            {
                _logger.LogDebug(
                    "Ignoring API error - the attempt was done with cached key. Prompting user to enter new license key.");
            }
        }

        public override void Prepare([NotNull] UpdaterStatus status, CancellationToken cancellationToken)
        {
            base.Prepare(status, cancellationToken);
            
            if (status == null) throw new ArgumentNullException("status");
        }

        public string KeySecret { get; private set; }

        private void SetCachedKey(string value)
        {
            _localMetaData.SetProductKey(value);
        }

        private string GetCachedKey()
        {
            return _localMetaData.GetProductKey();
        }

        private void SetCachedKeySecret(string key, string value)
        {
            _cache.SetValue(CachePatchKitKeySecret + key, value);
        }

        private string GetCachedKeySecret(string key)
        {
            return _cache.GetValue(CachePatchKitKeySecret + key);
        }
    }
}