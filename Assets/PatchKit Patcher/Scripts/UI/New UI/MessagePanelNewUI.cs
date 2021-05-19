﻿using UniRx;
using PatchKit.Unity.UI.Languages;
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

        public Text CheckButtonText;

        public ITextTranslator checkButtonTextMeshProTranslator;

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
            if (checkButtonTextMeshProTranslator == null)
                checkButtonTextMeshProTranslator = CheckButtonText.gameObject.AddComponent<TextTranslator>();

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
                    checkButtonTextMeshProTranslator.SetText(PatcherLanguages.OpenTag + "install" + PatcherLanguages.CloseTag);
                }
                
                CheckButton.gameObject.SetActive(_canInstallApp);
            }).AddTo(this);
            
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