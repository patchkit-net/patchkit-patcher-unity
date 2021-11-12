using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class NewChangelogElement : MonoBehaviour
    {
        public ITextTranslator Text;

        private void Awake()
        {
            Text = GetComponentInChildren<ITextTranslator>();
        }
    }
}