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

        private void Update()
        {
            var state = PatcherApplication.Instance.Patcher.Status.State;

            if (state == PatcherState.None || state == PatcherState.Cancelled || state == PatcherState.Failed)
            {
                _animator.SetBool("IsOpened", true);
                ButtonText.text = "Retry";
            }
            else if (state == PatcherState.Patching)
            {
                _animator.SetBool("IsOpened", false);
            }
            else if (state == PatcherState.Succeed)
            {
                _animator.SetBool("IsOpened", true);
                ButtonText.text = "Play";
            }
        }

        public void Action()
        {
            var state = PatcherApplication.Instance.Patcher.Status.State;

            if (state == PatcherState.None || state == PatcherState.Cancelled || state == PatcherState.Failed)
            {
                PatcherApplication.Instance.RetryPatching();
            }
            else if (state == PatcherState.Succeed)
            {
                PatcherApplication.Instance.StartApplicationAndQuit();
            }
        }
    }
}