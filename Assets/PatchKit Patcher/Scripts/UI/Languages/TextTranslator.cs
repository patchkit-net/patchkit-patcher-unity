using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.UI.Languages
{
    [RequireComponent (typeof(Text))]
    public class TextTranslator : MonoBehaviour
    {
        public string Key;
        private Text _text;

        void Awake ()
        {
            _text = GetComponent<Text>();
        }
        
        void Start ()
        {
            _text.text = PatcherLanguages.GetTraduction (Key);
        }
    }
}
