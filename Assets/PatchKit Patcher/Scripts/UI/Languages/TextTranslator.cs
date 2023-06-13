using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.UI.Languages
{
    [RequireComponent(typeof(Text))]
    public class TextTranslator : MonoBehaviour, ITextTranslator
    {
        public string Key;
        private Text _textComponent;
        private string _language;
        private string _text;
        private object[] _args;

        void Start()
        {
            _textComponent = GetComponent<Text>();
            _language = PatcherLanguages.language;
            if (!string.IsNullOrEmpty(Key))
            {
                _textComponent.text = PatcherLanguages.GetTranslation(Key);
            }

            if (!string.IsNullOrEmpty(_text))
            {
                _textComponent.text = PatcherLanguages.GetTranslationText(string.Format(_text, _args));
            }
        }

        void Update()
        {
            if (PatcherLanguages.language == _language)
            {
                return;
            }

            _language = PatcherLanguages.language;
            if (string.IsNullOrEmpty(_text))
            {
                _textComponent.text = PatcherLanguages.GetTranslation(Key);
            }
            else
            {
                _textComponent.text = _args != null
                    ? PatcherLanguages.GetTranslationText(_text, _args)
                    : PatcherLanguages.GetTranslationText(_text);
            }
        }

        public void SetText(string text, params object[] args)
        {
            _args = args;
            _text = string.IsNullOrEmpty(text) ? "" : text;

            _language = null;
        }
    }
}