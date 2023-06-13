using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    [RequireComponent(typeof(Animator))]
    public class DownloadPanel : MonoBehaviour
    {
        public Button BackgroundShowDownload;
        public Transform MessagePanel;
        public Transform MessagePanelInDownloadPanel;
        public Transform ProgressBar;
        public GameObject WhenUpdatingPanel;
        public GameObject StaticPanel;
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
                    ColorBlock cb = BackgroundShowDownload.colors;
                    cb.normalColor = Color.white;
                    BackgroundShowDownload.colors = cb;
                    ProgressBar.SetParent(MessagePanelInDownloadPanel, false);
                }
                else
                {
                    ColorBlock cb = BackgroundShowDownload.colors;
                    cb.normalColor = Color.clear;
                    BackgroundShowDownload.colors = cb;
                    ProgressBar.SetParent(MessagePanel, false);
                }
            }
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            Patcher.Instance.State.ObserveOnMainThread().Subscribe(ChangeUIOnUpdatingApp);
        }

        private void ChangeUIOnUpdatingApp(PatcherState state)
        {
            if (state == PatcherState.UpdatingApp)
            {
                WhenUpdatingPanel.SetActive(true);
                StaticPanel.SetActive(false);
            }
            else
            {
                WhenUpdatingPanel.SetActive(false);
                StaticPanel.SetActive(true);
            }
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

