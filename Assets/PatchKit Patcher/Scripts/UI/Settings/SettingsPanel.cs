using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    [RequireComponent(typeof(Animator))]
    public class SettingsPanel : MonoBehaviour
    {
        public Button BackgroundShowSettings;
        private Animator _animator;

        private bool _isOpened;
        private readonly Color _color = new Color32(0, 116, 228, 255);

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
                    ColorBlock cb = BackgroundShowSettings.colors;
                    cb.normalColor = _color;
                    BackgroundShowSettings.colors = cb;
                }
                else
                {
                    transform.localPosition= new Vector3(0, -671, 0);
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

