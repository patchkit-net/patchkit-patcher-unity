using PatchKit.Unity.UI.Languages;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI
{
    public class WarningText : MonoBehaviour
    {
        private ITextTranslator _textMeshProTranslator;

        private void Start()
        {
            _textMeshProTranslator = GetComponent<ITextTranslator>() ?? gameObject.AddComponent<TextTranslator>();
            
            Patcher.Instance.Warning.ObserveOnMainThread().Subscribe(warning =>
            {
                _textMeshProTranslator.SetText(warning);
            }).AddTo(this);
        }
    }
}