using System.Collections.Generic;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class NewChangelogElement : MonoBehaviour
    {
        public List<ITextTranslator> Texts = new List<ITextTranslator>();

        private void Awake()
        {
            foreach (ITextTranslator textTranslator in GetComponentsInChildren<ITextTranslator>())
            {
                Texts.Add(textTranslator);
            }
        }
    }
}