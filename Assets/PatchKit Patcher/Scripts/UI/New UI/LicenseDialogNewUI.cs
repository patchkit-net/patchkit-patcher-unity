using System;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class LicenseDialogNewUI : BaseLicenseDialog
    {
        private LicenseDialogResult _result;
        
        public TextTranslator errorMessageTextTranslator;

        public InputField KeyInputField;

        private void Start()
        {
            Assert.IsNotNull(errorMessageTextTranslator);
            Assert.IsNotNull(KeyInputField);
        }

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

        public override void SetKey(string key)
        {
            UnityDispatcher.Invoke(() => KeyInputField.text = key);
        }

        public override LicenseDialogResult Display(LicenseDialogMessageType messageType)
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
                    errorMessageTextTranslator.SetText(string.Empty);
                    break;
                case LicenseDialogMessageType.InvalidLicense:
                    errorMessageTextTranslator.SetText(LanguageHelper.Tag("invalid_license"));
                    break;
                case LicenseDialogMessageType.BlockedLicense:
                    errorMessageTextTranslator.SetText(LanguageHelper.Tag("blocked_license"));
                    break;
                case LicenseDialogMessageType.ServiceUnavailable:
                    errorMessageTextTranslator.SetText(LanguageHelper.Tag("service_is_unavailable"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException("messageType", messageType, null);
            }
        }
    }
}