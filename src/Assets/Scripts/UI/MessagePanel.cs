using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher
{
    [RequireComponent(typeof(Animator))]
    public class MessagePanel : MonoBehaviour
    {
        public Text ButtonText;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            PatcherApplication.Instance.Patcher.OnStateChanged += state =>
            {
                if (state == PatcherState.None)
                {
                    _animator.SetBool("IsOpened", true);
                    ButtonText.text = "Patch";
                }
                else if (state == PatcherState.Cancelled || state == PatcherState.Error || state == PatcherState.UnauthorizedAccess)
                {
                    _animator.SetBool("IsOpened", true);
                    ButtonText.text = "Retry";
                }
                else if (state == PatcherState.Processing)
                {
                    _animator.SetBool("IsOpened", false);
                }
                else if (state == PatcherState.Success)
                {
                    _animator.SetBool("IsOpened", true);
                    ButtonText.text = "Play";
                }
            };
        }

        public void Action()
        {
            var state = PatcherApplication.Instance.Patcher.State;

            if (state == PatcherState.None || state == PatcherState.Cancelled || state == PatcherState.Error || state == PatcherState.UnauthorizedAccess)
            {
                PatcherApplication.Instance.RetryPatching();
            }
            else if (state == PatcherState.Success)
            {
                PatcherApplication.Instance.StartApplicationAndQuit();
            }
        }
    }
}