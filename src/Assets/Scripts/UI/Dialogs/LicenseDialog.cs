using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class LicenseDialog : MonoBehaviour, ILicenseDialog
    {
        private readonly ManualResetEvent _dialogResultChangedEvent = new ManualResetEvent(false);

        private LicenseDialogResult _result;

        private bool _isDisplaying;

        private Animator _animator;

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

            _dialogResultChangedEvent.Set();
        }

        public void Abort()
        {
            _result = new LicenseDialogResult
            {
                Key = null,
                Type = LicenseDialogResultType.Aborted
            };

            _dialogResultChangedEvent.Set();
        }

        public LicenseDialogResult Display(LicenseDialogMessageType messageType)
        {
            try
            {
                _isDisplaying = true;

                UpdateMessage(messageType);

                _dialogResultChangedEvent.Reset();
                _dialogResultChangedEvent.WaitOne();

                return _result;
            }
            finally
            {
                _isDisplaying = false;
            }
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

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            _animator.SetBool("IsOpened", _isDisplaying);
        }
    }
}