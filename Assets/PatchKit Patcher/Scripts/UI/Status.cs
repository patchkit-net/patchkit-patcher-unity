using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI
{
    public class Status : MonoBehaviour
    {
        private ITextTranslator _textTranslator;

        private void Start()
        {
            _textTranslator = GetComponent<ITextTranslator>();
            if (_textTranslator == null)
                _textTranslator = gameObject.AddComponent<TextTranslator>();

            var operationStatus = Patcher.Instance.UpdaterStatus.SelectSwitchOrNull(s => s.LatestActiveOperation);

            var statusDescription = operationStatus.SelectSwitchOrDefault(s => s.Description, string.Empty);


            Patcher.Instance.State.CombineLatest(statusDescription, (state, description) =>
                {
                    switch (state)
                    {
                        case PatcherState.None:
                            return string.Empty;
                        case PatcherState.LoadingPatcherData:
                            return LanguageHelper.Tag("loading_data");
                        case PatcherState.LoadingPatcherConfiguration:
                            return LanguageHelper.Tag("loading_configuration");
                        case PatcherState.WaitingForUserDecision:
                            return string.Empty;
                        case PatcherState.StartingApp:
                            return LanguageHelper.Tag("starting_application");
                        case PatcherState.UpdatingApp:
                            return description;
                    }

                    return string.Empty;
                }).ObserveOnMainThread().Subscribe(textTranslation => _textTranslator.SetText(textTranslation))
                .AddTo(this);
        }
    }
}