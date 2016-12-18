using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.Licensing
{
    public class KeyLicenseObtainer : MonoBehaviour, ILicenseObtainer
    {
        private State _state = State.None;

        private KeyLicense _keyLicense;

        private Animator _animator;

        public InputField KeyInputField;

        public GameObject ErrorMessage;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            _animator.SetBool("IsOpened", _state == State.Obtaining);

            ErrorMessage.SetActive(ShowError);
        }

        public void Cancel()
        {
            _state = State.Cancelled;
        }

        public void Confirm()
        {
            string key = KeyInputField.text;
            key = key.ToUpper().Trim();

            _keyLicense = new KeyLicense
            {
                Key = key
            };

            _state = State.Confirmed;
        }

        public bool ShowError { get; set; }

        ILicense ILicenseObtainer.Obtain()
        {
            _state = State.Obtaining;

            while (_state != State.Confirmed && _state != State.Cancelled)
            {
                Thread.Sleep(10);
            }

            if (_state == State.Cancelled)
            {
                throw new OperationCanceledException();
            }

            return _keyLicense;
        }

        private enum State
        {
            None,
            Obtaining,
            Confirmed,
            Cancelled
        }
    }
}