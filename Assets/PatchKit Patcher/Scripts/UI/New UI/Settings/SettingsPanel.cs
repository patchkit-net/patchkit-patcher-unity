using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    [RequireComponent(typeof(Animator))]
    public class SettingsPanel : MonoBehaviour
    {
        public Button BackgroundShowSettings;
        private Animator _animator;

        private bool _isOpened;

        public bool IsOpened
        {
            get { return _isOpened; }
            set
            {
                if (value == _isOpened)
                {
                    return;
                }

                _isOpened = value;
                _animator.SetBool("IsOpened", _isOpened);
                if (_isOpened)
                {
                    ColorBlock cb = BackgroundShowSettings.colors;
                    cb.normalColor = Color.white;
                    BackgroundShowSettings.colors = cb;
                }
                else
                {
                    ColorBlock cb = BackgroundShowSettings.colors;
                    cb.normalColor = Color.clear;
                    BackgroundShowSettings.colors = cb;
                }
            }
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            IsOpened = false;
        }

        public void Open()
        {
            IsOpened = true;
        }

        public void Close()
        {
            IsOpened = false;
        }

        public void OpenClose()
        {
            if (_isOpened)
            {
                Close();
            }
            else
            {
                Open();
            }
        }
    }
}

