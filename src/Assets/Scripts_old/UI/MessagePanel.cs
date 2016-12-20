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
                if (state == PatcherState.Processing)
                {
                    _animator.SetBool("IsOpened", false);
                }
                else if (PatcherApplication.Instance.Patcher.CanPlay)
                {
                    _animator.SetBool("IsOpened", true);
                    ButtonText.text = "Play";
                }
                else
                {
                    _animator.SetBool("IsOpened", true);
                    ButtonText.text = "Retry";
                }
            };
        }

        public void Action()
        {
            if (PatcherApplication.Instance.Patcher.CanPlay)
            {
                PatcherApplication.Instance.StartApplicationAndQuit();
            }
            else
            {
                PatcherApplication.Instance.RetryPatching();
            }
        }
    }
}