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
			Patcher.Instance.StateChanged += (state) =>
			{
				_animator.SetBool("IsOpened", state == PatcherState.HandlingErrorMessage);
			};

			Patcher.Instance.ErrorChanged += (error) =>
			{
				if(error == null)
				{
					ErrorText.text = string.Empty;
				}
				else
				{
					ErrorText.text = "An error has occurred!";
				}
			};
		}
	}
}