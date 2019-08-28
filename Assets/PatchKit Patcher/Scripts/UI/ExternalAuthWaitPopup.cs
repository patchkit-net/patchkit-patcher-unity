using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI
{
    [RequireComponent(typeof(Animator))]
    public class ExternalAuthWaitPopup : MonoBehaviour
    {
        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            Patcher.Instance.HasOngoingExternalAuth.ObserveOnMainThread().Subscribe(value =>
            {
                _animator.SetBool("IsOpened", value);
            }).AddTo(this);
        }
    }
}