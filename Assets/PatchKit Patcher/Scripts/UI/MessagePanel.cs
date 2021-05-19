using UniRx;
using PatchKit.Unity.UI.Languages;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    [RequireComponent(typeof(Animator))]
    public class MessagePanel : MonoBehaviour
    {
        public string StartAppCustomArgs;

        public Button PlayButton;

        public Button CheckButton;

        public Text CheckButtonText;

        public ITextTranslator checkButtonTextMeshProTranslator;

        private bool _canInstallApp;

        private bool _canCheckForAppUpdates;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            if (checkButtonTextMeshProTranslator == null)
                checkButtonTextMeshProTranslator = CheckButtonText.gameObject.AddComponent<TextTranslator>();

            Patcher.Instance.State.ObserveOnMainThread().Subscribe(state =>
            {
                _animator.SetBool("IsOpened", state == PatcherState.WaitingForUserDecision);
            }).AddTo(this);

            Patcher.Instance.CanStartApp.ObserveOnMainThread().Subscribe(canStartApp =>
            {
                PlayButton.interactable = canStartApp;
            }).AddTo(this);

            Patcher.Instance.CanInstallApp.ObserveOnMainThread().Subscribe(canInstallApp =>
            {
                _canInstallApp = canInstallApp;
                if (_canInstallApp)
                {
                    checkButtonTextMeshProTranslator.SetText(PatcherLanguages.OpenTag + "install" + PatcherLanguages.CloseTag);
                }

                CheckButton.interactable = _canInstallApp || _canCheckForAppUpdates;
            }).AddTo(this);

            Patcher.Instance.CanCheckForAppUpdates.ObserveOnMainThread().Subscribe(canCheckForAppUpdates =>
            {
                _canCheckForAppUpdates = canCheckForAppUpdates;
                if (_canCheckForAppUpdates)
                {
                    checkButtonTextMeshProTranslator.SetText(PatcherLanguages.OpenTag + "check_for_updates" +
                                                      PatcherLanguages.CloseTag);
                }

                CheckButton.interactable = _canInstallApp || _canCheckForAppUpdates;
            }).AddTo(this);

            PlayButton.onClick.AddListener(OnPlayButtonClicked);
            CheckButton.onClick.AddListener(OnCheckButtonClicked);

            _animator.SetBool("IsOpened", false);
            PlayButton.interactable = false;
            CheckButton.interactable = false;
            
            checkButtonTextMeshProTranslator.SetText(
                PatcherLanguages.OpenTag + "check_for_updates" + PatcherLanguages.CloseTag);
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