using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace PatchKit.Unity.UI.Languages
{
    [RequireComponent(typeof(Dropdown))]
    public class DropdownLanguages : MonoBehaviour
    {
        private static Dropdown _dropdown;
        private static int _lastOption;

        void Awake()
        {
            _dropdown = GetComponent<Dropdown>();
            _lastOption = _dropdown.options.Count - 1;
            _dropdown.value = _lastOption;
            _dropdown.onValueChanged.AddListener(delegate {
                DropdownValueChanged(_dropdown);
            });
        }

        public static void SetValue(string language)
        {
            int currentLanguages = _dropdown.options.FindIndex(id => id.image.name == language);
            var options = _dropdown.options;
            Dropdown.OptionData tmp = options[currentLanguages];
            options[currentLanguages] = options[_lastOption];
            options[_lastOption] = tmp;
        }

        void DropdownValueChanged(Dropdown change)
        {
            int currentLanguages = change.value;
            var options = change.options;
            Dropdown.OptionData tmp = options[currentLanguages];
            options[currentLanguages] = options[_lastOption];
            options[_lastOption] = tmp;
            PatcherLanguages.SetLanguage(tmp.image.name);
            _dropdown.value = _lastOption;
        }
    }
}
