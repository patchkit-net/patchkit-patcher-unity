using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    [RequireComponent(typeof(Animator))]
    public class SettingsPanel : MonoBehaviour
    {
        public Image BackgroundShowSettings;
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
                if (_isOpened)
                {
                    transform.localPosition= new Vector3(0, -24, 0);
                    BackgroundShowSettings.enabled = true;
                }
                else
                {
                    transform.localPosition= new Vector3(0, -671, 0);
                    BackgroundShowSettings.enabled = false;
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

