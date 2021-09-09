using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    [RequireComponent(typeof(Animator))]
    public class NewChangelogPanel : MonoBehaviour
    {
        private Animator _animator;

        private bool _isOpened;

        public GameObject dotNewVersion;

        public RealeasesList RealeasesList;

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
            }
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            IsOpened = false;
            
            Patcher.Instance.CanInstallApp.ObserveOnMainThread().Subscribe(canInstallApp =>
            {
                dotNewVersion.SetActive(canInstallApp);
            }).AddTo(this);
        }

        public void Open()
        {
            IsOpened = true;
            RealeasesList.PrepareReleases();
        }

        public void Close()
        {
            IsOpened = false;
        }
    }
}