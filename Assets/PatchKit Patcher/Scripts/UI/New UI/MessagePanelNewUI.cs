using PatchKit.Unity.Patcher.Debug;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class MessagePanelNewUI : MonoBehaviour
    {
        public string StartAppCustomArgs;

        public Button PlayButton;

        public Button InstallButton;
        
        public Button UpdateButton;
        
        public GameObject ProgressBar;

        private bool _canInstallApp;

        private bool _canCheckForAppUpdates;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            Assert.IsNotNull(UpdateButton);
            Assert.IsNotNull(ProgressBar);
            Assert.IsNotNull(PlayButton);
            Assert.IsNotNull(InstallButton);
            Assert.IsNotNull(_animator);

            Patcher.Instance.State.ObserveOnMainThread().Subscribe(state =>
            {
                _animator.SetBool("IsOpened", state == PatcherState.WaitingForUserDecision);
                ProgressBar.SetActive(state == PatcherState.UpdatingApp);
            }).AddTo(this);

            Patcher.Instance.CanStartApp.ObserveOnMainThread().Subscribe(canStartApp =>
            {
                PlayButton.gameObject.SetActive(canStartApp);
            }).AddTo(this);

            Patcher.Instance.CanInstallApp.ObserveOnMainThread().Subscribe(canInstallApp =>
            {
                _canInstallApp = canInstallApp;

                InstallButton.gameObject.SetActive(_canInstallApp);
            }).AddTo(this);
            
            Patcher.Instance.CanCheckForAppUpdates.ObserveOnMainThread().Subscribe(canCheckForAppUpdates =>
            {
                _canCheckForAppUpdates = canCheckForAppUpdates;
                
                UpdateButton.gameObject.SetActive(_canCheckForAppUpdates);
            }).AddTo(this);

            PlayButton.onClick.AddListener(OnPlayButtonClicked);
            InstallButton.onClick.AddListener(OnCheckButtonClicked);
            UpdateButton.onClick.AddListener(OnCheckButtonClicked);

            _animator.SetBool("IsOpened", false);
        }

        private void OnPlayButtonClicked()
        {
            Patcher.Instance.StartAppCustomArgs = StartAppCustomArgs;
            Patcher.Instance.SetUserDecision(Patcher.UserDecision.StartApp);
        }

        private void OnCheckButtonClicked()
        {
            if (_canInstallApp)
            {
                Patcher.Instance.SetUserDecision(Patcher.UserDecision.InstallApp);
            }
            else if (_canCheckForAppUpdates)
            {
                Patcher.Instance.SetUserDecision(Patcher.UserDecision.CheckForAppUpdates);
            }
        }
    }
}