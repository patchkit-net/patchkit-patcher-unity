using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    [RequireComponent(typeof(Animator))]
    public class ErrorPanel : MonoBehaviour
    {
        public Text ErrorText;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            Patcher.Instance.State.ObserveOnMainThread().Subscribe(state =>
            {
                _animator.SetBool("IsOpened", state == PatcherState.HandlingErrorMessage);
            }).AddTo(this);

            Patcher.Instance.Error.ObserveOnMainThread().Subscribe(error =>
            {
                ErrorText.text = error == null ? string.Empty : "An error has occurred!";
            }).AddTo(this);

            _animator.SetBool("IsOpened", false);
            ErrorText.text = string.Empty;
        }
    }
}