﻿using TMPro;
using UnityEngine;

namespace PatchKit.Unity.UI.Languages
{
    [RequireComponent(typeof(TextMeshPro))]
    public class TextMeshProTranslator : MonoBehaviour, ITextTranslator
    {
        public string Key;
        private TextMeshProUGUI _textComponent;
        private string _language;
        private string _text;
        private object[] _args;

        void Start()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
            _language = PatcherLanguages.language;
            if(!string.IsNullOrEmpty(Key))
                _textComponent.text = PatcherLanguages.GetTranslation(Key);
            if(!string.IsNullOrEmpty(_text))
                _textComponent.text = PatcherLanguages.GetTranslationText(string.Format(_text, _args));
        }

        void Update()
        {
            if (PatcherLanguages.language != _language)
            {
                _language = PatcherLanguages.language;
                if(string.IsNullOrEmpty(_text))
                {
                    _textComponent.text = PatcherLanguages.GetTranslation(Key);
                }
                else
                {
                    if(_args != null)
                        _textComponent.text = PatcherLanguages.GetTranslationText(string.Format(_text, _args));
                    else
                        _textComponent.text = PatcherLanguages.GetTranslationText(_text);
                }
            }
        }

        public void SetText(string text, params object[] args)
        {
            _args = args;
            if (string.IsNullOrEmpty(text))
            {
                _text = "";
            }
            else
            {
                _text = text;
            }
            _language = null;
        }
    }
}
