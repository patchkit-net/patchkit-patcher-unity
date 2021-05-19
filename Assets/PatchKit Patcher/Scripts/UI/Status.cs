﻿using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI
{
    [RequireComponent(typeof(ITextTranslator))]
    public class Status : MonoBehaviour
    {
        private ITextTranslator _textMeshProTranslator;

        private void Start()
        {
            _textMeshProTranslator = GetComponent<ITextTranslator>();
            if (_textMeshProTranslator == null)
                _textMeshProTranslator = gameObject.AddComponent<TextTranslator>();

            var operationStatus = Patcher.Instance.UpdaterStatus.SelectSwitchOrNull(s => s.LatestActiveOperation);

            var statusDescription = operationStatus.SelectSwitchOrDefault(s => s.Description, string.Empty);


            Patcher.Instance.State.CombineLatest(statusDescription, (state, description) =>
                {
                    switch (state)
                    {
                        case PatcherState.None:
                            return string.Empty;
                        case PatcherState.LoadingPatcherData:
                            return PatcherLanguages.OpenTag + "loading_data" + PatcherLanguages.CloseTag;
                        case PatcherState.LoadingPatcherConfiguration:
                            return PatcherLanguages.OpenTag + "loading_configuration" + PatcherLanguages.CloseTag;
                        case PatcherState.WaitingForUserDecision:
                            return string.Empty;
                        case PatcherState.StartingApp:
                            return PatcherLanguages.OpenTag + "starting_application" + PatcherLanguages.CloseTag;
                        case PatcherState.UpdatingApp:
                            return description;
                    }

                    return string.Empty;
                }).ObserveOnMainThread().Subscribe(textTranslation => _textMeshProTranslator.SetText(textTranslation))
                .AddTo(this);
        }
    }
}