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

            DebugLogger.Log("Validating license.");

            KeySecret = null;

            var appInfo = _remoteMetaData.GetAppInfo();

            if (!appInfo.UseKeys)
            {
                DebugLogger.Log("Application is not using license keys.");
                return;
            }

            LicenseDialogMessageType messageType = LicenseDialogMessageType.None;

            while (KeySecret == null)
            {
                DebugLogger.Log("Displaying license dialog.");

                var result = _licenseDialog.Display(messageType);

                DebugLogger.Log("Processing dialog result.");

                if (result.Type == LicenseDialogResultType.Confirmed)
                {
                    try
                    {
                        KeySecret = _remoteMetaData.GetKeySecret(result.Key);

                        DebugLogger.LogVariable(KeySecret, "KeySecret");
                        DebugLogger.Log("License key has been validated");
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