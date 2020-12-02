using System;
using PatchKit.Unity.UI.Languages;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    [RequireComponent(typeof(TextTranslator))]
    public class WarningText : MonoBehaviour
    {
        private TextTranslator _textTranslator;

        private void Start()
        {
            _textTranslator = GetComponent<TextTranslator>();
            if (_textTranslator == null)
                _textTranslator = gameObject.AddComponent<TextTranslator>();
            Patcher.Instance.Warning.ObserveOnMainThread().Subscribe(warning =>
            {
                _textTranslator.SetText(warning);
            }).AddTo(this);
        }
    }
}