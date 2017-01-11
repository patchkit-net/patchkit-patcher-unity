using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
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
            /*Patcher.Instance.Patcher.OnStateChanged += state =>
            {
                if (state == PatcherState.Processing)
                {
                    _animator.SetBool("IsOpened", false);
                }
                else if (Patcher.Instance.Patcher.CanPlay)
                {
                    _animator.SetBool("IsOpened", true);
                    ButtonText.text = "Play";
                }
                else
                {
                    _animator.SetBool("IsOpened", true);
                    ButtonText.text = "Retry";
                }
            };*/
        }

        public void Action()
        {
            /*if (Patcher.Instance.Patcher.CanPlay)
            {
                Patcher.Instance.StartApplicationAndQuit();
            }
            else
            {
                Patcher.Instance.RetryPatching();
            }*/
        }
    }
}