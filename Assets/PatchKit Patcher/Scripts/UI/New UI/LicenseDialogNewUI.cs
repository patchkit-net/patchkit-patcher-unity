using System;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class LicenseDialogNewUI : ALicenseDialog
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
                    errorMessageTextTranslator.SetText(string.Empty);
                    break;
                case LicenseDialogMessageType.InvalidLicense:
                    errorMessageTextTranslator.SetText(PatcherLanguages.OpenTag + "invalid_license" +
                                                       PatcherLanguages.CloseTag);
                    break;
                case LicenseDialogMessageType.BlockedLicense:
                    errorMessageTextTranslator.SetText(PatcherLanguages.OpenTag + "blocked_license" +
                                                       PatcherLanguages.CloseTag);
                    break;
                case LicenseDialogMessageType.ServiceUnavailable:
                    errorMessageTextTranslator.SetText(PatcherLanguages.OpenTag + "service_is_unavailable" +
                                                       PatcherLanguages.CloseTag);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("messageType", messageType, null);
            }
        }
    }
}