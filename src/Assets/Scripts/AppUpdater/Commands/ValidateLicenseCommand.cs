using System;
using System.Net;
using PatchKit.Api;
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

        public ValidateLicenseCommand(ILicenseDialog licenseDialog, IRemoteMetaData remoteMetaData)
        {
            AssertChecks.ArgumentNotNull(licenseDialog, "licenseDialog");
            AssertChecks.ArgumentNotNull(remoteMetaData, "remoteMetaData");

            DebugLogger.LogConstructor();

            _licenseDialog = licenseDialog;
            _remoteMetaData = remoteMetaData;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            KeySecret = null;

            var appInfo = _remoteMetaData.GetAppInfo();

            if (!appInfo.UseKeys)
            {
                return;
            }

            LicenseDialogMessageType messageType = LicenseDialogMessageType.None;

            while (KeySecret == null)
            {
                var result = _licenseDialog.Display(messageType);

                if (result.Type == LicenseDialogResultType.Confirmed)
                {
                    try
                    {
                        KeySecret = _remoteMetaData.GetKeySecret(result.Key);
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
                    }
                }
                else if (result.Type == LicenseDialogResultType.Aborted)
                {
                    throw new OperationCanceledException();
                }
            }
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");
        }

        public string KeySecret { get; private set; }
    }
}