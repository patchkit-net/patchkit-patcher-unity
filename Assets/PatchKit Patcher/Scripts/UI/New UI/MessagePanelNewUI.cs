using System;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Debug;
using UniRx;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class MessagePanelNewUI : MonoBehaviour
    {
        public string StartAppCustomArgs;

        public Button PlayButton;

        public Button CheckButton;

        public TextMeshProTranslator checkButtonTextMeshProTranslator;

        public TextMeshProTranslator sizeTextMeshProTranslator;

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
            Assert.IsNotNull(checkButtonTextMeshProTranslator);
            Assert.IsNotNull(ProgressBar);
            Assert.IsNotNull(PlayButton);
            Assert.IsNotNull(CheckButton);
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

                if (_canInstallApp)
                {
                    checkButtonTextMeshProTranslator.SetText(PatcherLanguages.OpenTag + "install" +
                                                             PatcherLanguages.CloseTag);
                }

                CheckButton.gameObject.SetActive(_canInstallApp);
            }).AddTo(this);

            var text = Patcher.Instance.SizeLastContentSummary.Select(
                sizeLastContentSummary =>
                {
                    if (sizeLastContentSummary != 0)
                    {
                        return string.Format("({0:0.0}MB)", sizeLastContentSummary / 1024.0 / 1024.0);
                    }
                    return String.Empty;
                });

            text.ObserveOnMainThread().Subscribe(t => sizeTextMeshProTranslator.SetText(t)).AddTo(this);
            
            PlayButton.onClick.AddListener(OnPlayButtonClicked);
            CheckButton.onClick.AddListener(OnCheckButtonClicked);

            _animator.SetBool("IsOpened", false);

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