using TMPro;
using UnityEngine;

namespace PatchKit.Unity.UI.Languages
{
    [RequireComponent(typeof(TextMeshProUGUI))]
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
                _textComponent.text = string.Format(PatcherLanguages.GetTranslationText(_text), _args);
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
                    _textComponent.text = string.Format(PatcherLanguages.GetTranslationText(_text), _args);
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
