using System;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.UI.Languages;
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

        public void SetKey(string key)
        {
            UnityDispatcher.Invoke(() => KeyInputField.text = key);
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
                    ErrorMessageText.text = PatcherLanguages.GetTranslation("invalid_license");
                    break;
                case LicenseDialogMessageType.BlockedLicense:
                    ErrorMessageText.text = PatcherLanguages.GetTranslation("blocked_license");
                    break;
                case LicenseDialogMessageType.ServiceUnavailable:
                    ErrorMessageText.text = PatcherLanguages.GetTranslation("service_is_unavailable");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("messageType", messageType, null);
            }
        }
    }
}