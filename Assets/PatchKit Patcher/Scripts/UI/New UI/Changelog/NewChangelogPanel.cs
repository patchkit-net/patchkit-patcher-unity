using System;
using PatchKit.Unity.Utilities;
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
            
            Patcher.Instance.CanCheckForAppUpdates
                .CombineLatest(Patcher.Instance.CanInstallApp,
                    (canCheckForAppUpdates, canInstallApp) => canCheckForAppUpdates || canInstallApp)
                .ObserveOnMainThread()
                .Subscribe(b => dotNewVersion.SetActive(b));
        }

        public void Open()
        {
            IsOpened = true;
        }

        public void Close()
        {
            IsOpened = false;
        }
    }
}