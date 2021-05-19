using System;
using PatchKit.Unity.UI.Languages;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    [RequireComponent(typeof(ITextTranslator))]
    public class WarningText : MonoBehaviour
    {
        private ITextTranslator _textMeshProTranslator;

        private void Start()
        {
            _textMeshProTranslator = GetComponent<ITextTranslator>();
            if (_textMeshProTranslator == null)
                _textMeshProTranslator = gameObject.AddComponent<TextTranslator>();
            Patcher.Instance.Warning.ObserveOnMainThread().Subscribe(warning =>
            {
                _textMeshProTranslator.SetText(warning);
            }).AddTo(this);
        }
    }
}