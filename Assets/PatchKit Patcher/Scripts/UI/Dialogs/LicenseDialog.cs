using System;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class LicenseDialog : Dialog<LicenseDialog>, ILicenseDialog
    {
        private LicenseDialogResult _result;

        public Text ErrorMessageText;

        public InputField KeyInputField;

        [Multiline]
        public string InvalidLicenseMessageText;

        [Multiline]
        public string BlockedLicenseMessageText;

        [Multiline]
        public string ServiceUnavailableMessageText;

        public void Confirm()
        {
            string key = KeyInputField.text;
            key = key.ToUpper().Trim();

            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            _result = new LicenseDialogResult
            {
                Key = key,
                Type = LicenseDialogResultType.Confirmed
            };

            base.OnDisplayed();
        }

        public void Abort()
        {
            _result = new LicenseDialogResult
            {
                Key = null,
                Type = LicenseDialogResultType.Aborted
            };

            base.OnDisplayed();
        }

        public LicenseDialogResult Display(LicenseDialogMessageType messageType)
        {
            UnityDispatcher.Invoke(() => UpdateMessage(messageType));

            base.Display(CancellationToken.Empty);

            return _result;
        }

        private void UpdateMessage(LicenseDialogMessageType messageType)
        {
            switch (messageType)
            {
                case LicenseDialogMessageType.None:
                    ErrorMessageText.text = string.Empty;
                    break;
                case LicenseDialogMessageType.InvalidLicense:
                    ErrorMessageText.text = InvalidLicenseMessageText;
                    break;
                case LicenseDialogMessageType.BlockedLicense:
                    ErrorMessageText.text = BlockedLicenseMessageText;
                    break;
                case LicenseDialogMessageType.ServiceUnavailable:
                    ErrorMessageText.text = ServiceUnavailableMessageText;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("messageType", messageType, null);
            }
        }
    }
}