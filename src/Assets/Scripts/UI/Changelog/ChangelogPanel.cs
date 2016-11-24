using UnityEngine;

namespace PatchKit.Unity.Patcher.UI
{
    [RequireComponent(typeof(Animator))]
    public class ChangelogPanel : MonoBehaviour
    {
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
            }
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
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
    }
}