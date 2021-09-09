using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    [RequireComponent(typeof(Animator))]
    public class DownloadPanel : MonoBehaviour
    {
        public Button BackgroundShowDownload;
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
                    transform.localPosition= new Vector3(0, -24, 0);
                    ColorBlock cb = BackgroundShowDownload.colors;
                    cb.normalColor = Color.white;
                    BackgroundShowDownload.colors = cb;
                }
                else
                {
                    transform.localPosition= new Vector3(0, -671, 0);
                    ColorBlock cb = BackgroundShowDownload.colors;
                    cb.normalColor = Color.clear;
                    BackgroundShowDownload.colors = cb;
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

