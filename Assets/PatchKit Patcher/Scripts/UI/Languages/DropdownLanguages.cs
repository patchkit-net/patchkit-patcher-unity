using TMPro;
using UnityEngine;


namespace PatchKit.Unity.UI.Languages
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class DropdownLanguages : MonoBehaviour
    {
        private static TMP_Dropdown _dropdown;
        private static int _currentValue;

        void Awake()
        {
            _dropdown = GetComponent<TMP_Dropdown>();
            _currentValue = _dropdown.value;
            _dropdown.itemText.text = _dropdown.options[_currentValue].text;
            _dropdown.onValueChanged.AddListener(delegate {
                DropdownValueChanged(_dropdown);
            });
        }

        public static void SetValue(string language)
        {
            int currentLanguages = _dropdown.options.FindIndex(id => id.text.ToLower() == language);
            var options = _dropdown.options;
            TMP_Dropdown.OptionData tmp = options[currentLanguages];
            options[currentLanguages] = options[_currentValue];
            options[_currentValue] = tmp;
            _dropdown.itemText.text = _dropdown.options[_currentValue].text;
        }

        void DropdownValueChanged(TMP_Dropdown change)
        {
            int currentLanguages = change.value;
            var options = change.options;
            TMP_Dropdown.OptionData tmp = options[currentLanguages];
            options[currentLanguages] = options[_currentValue];
            options[_currentValue] = tmp;
            PatcherLanguages.SetLanguage(tmp.text.ToLower());
            _dropdown.value = _currentValue;
        }
    }
}
