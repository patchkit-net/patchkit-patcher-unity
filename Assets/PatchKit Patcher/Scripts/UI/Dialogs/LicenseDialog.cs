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
        public ITextTranslator errorMessageTextMeshProTranslator;

        public InputField KeyInputField;

        private void Start()
        {
            if (errorMessageTextMeshProTranslator == null)
                errorMessageTextMeshProTranslator = ErrorMessageText.gameObject.AddComponent<TextTranslator>();
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
                    errorMessageTextMeshProTranslator.SetText(string.Empty);
                    break;
                case LicenseDialogMessageType.InvalidLicense:
                    errorMessageTextMeshProTranslator.SetText(PatcherLanguages.OpenTag + "invalid_license" +
                                                       PatcherLanguages.CloseTag);
                    break;
                case LicenseDialogMessageType.BlockedLicense:
                    errorMessageTextMeshProTranslator.SetText(PatcherLanguages.OpenTag + "blocked_license" +
                                                       PatcherLanguages.CloseTag);
                    break;
                case LicenseDialogMessageType.ServiceUnavailable:
                    errorMessageTextMeshProTranslator.SetText(PatcherLanguages.OpenTag + "service_is_unavailable" +
                                                       PatcherLanguages.CloseTag);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("messageType", messageType, null);
            }
        }
    }
}