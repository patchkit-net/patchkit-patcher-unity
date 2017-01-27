using System;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class LicenseDialog : Dialog<LicenseDialog>, ILicenseDialog
    {
        private LicenseDialogResult _result;

        public Text MessageText;

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
            UpdateMessage(messageType);

            base.Display();

            return _result;
        }

        private void UpdateMessage(LicenseDialogMessageType messageType)
        {
            switch (messageType)
            {
                case LicenseDialogMessageType.None:
                    MessageText.text = string.Empty;
                    break;
                case LicenseDialogMessageType.InvalidLicense:
                    MessageText.text = InvalidLicenseMessageText;
                    break;
                case LicenseDialogMessageType.BlockedLicense:
                    MessageText.text = BlockedLicenseMessageText;
                    break;
                case LicenseDialogMessageType.ServiceUnavailable:
                    MessageText.text = ServiceUnavailableMessageText;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("messageType", messageType, null);
            }
        }
    }
}