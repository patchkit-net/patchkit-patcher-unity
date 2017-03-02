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
            Patcher.Instance.StateChanged += state =>
            {
                _animator.SetBool("IsOpened", state == PatcherState.HandlingErrorMessage);
            };

            Patcher.Instance.ErrorChanged += error =>
            {
                ErrorText.text = error == null ? string.Empty : "An error has occurred!";
            };

            _animator.SetBool("IsOpened", false);
            ErrorText.text = string.Empty;
        }
    }
}