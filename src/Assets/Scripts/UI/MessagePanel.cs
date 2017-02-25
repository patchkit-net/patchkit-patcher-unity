using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    [RequireComponent(typeof(Animator))]
    public class MessagePanel : MonoBehaviour
    {
        public Button PlayButton;

        public Button CheckButton;

        public Text CheckButtonText;

        private bool _canUpdateApp;

        private bool _canCheckInternetConnection;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            Patcher.Instance.State.ObserveOnMainThread().Subscribe(state =>
            {
                _animator.SetBool("IsOpened", state == PatcherState.WaitingForUserDecision);
            }).AddTo(this);

            Patcher.Instance.CanStartApp.ObserveOnMainThread().Subscribe(canStartApp =>
            {
                PlayButton.interactable = canStartApp;
            }).AddTo(this);

            Patcher.Instance.CanUpdateApp.ObserveOnMainThread().Subscribe(canUpdateApp =>
            {
                _canUpdateApp = canUpdateApp;
                if (_canUpdateApp)
                {
                    CheckButtonText.text = "Check for updates";
                }
                CheckButton.interactable = _canUpdateApp || _canCheckInternetConnection;
            }).AddTo(this);

            Patcher.Instance.CanCheckInternetConnection.ObserveOnMainThread().Subscribe(canCheckInternetConnection =>
            {
                _canCheckInternetConnection = canCheckInternetConnection;
                if (_canCheckInternetConnection)
                {
                    CheckButtonText.text = "Check internet connection";
                }
                CheckButton.interactable = _canUpdateApp || _canCheckInternetConnection;
            }).AddTo(this);

            PlayButton.onClick.AddListener(OnPlayButtonClicked);
            CheckButton.onClick.AddListener(OnCheckButtonClicked);

            _animator.SetBool("IsOpened", false);
            PlayButton.interactable = false;
            CheckButton.interactable = false;
            CheckButtonText.text = "Check for updates";
        }

        private void OnPlayButtonClicked()
        {
            Patcher.Instance.SetUserDecision(Patcher.UserDecision.StartApp);
        }

        private void OnCheckButtonClicked()
        {
            if(_canUpdateApp)
            {
                Patcher.Instance.SetUserDecision(Patcher.UserDecision.UpdateApp);
            }
            else if(_canCheckInternetConnection)
            {
                Patcher.Instance.SetUserDecision(Patcher.UserDecision.CheckInternetConnection);
            }
        }
    }
}