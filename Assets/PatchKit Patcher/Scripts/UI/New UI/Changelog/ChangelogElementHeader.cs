using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class ChangelogElementHeader : MonoBehaviour
    {
        public Transform Body;
        public GameObject ExpandButton;
        
        [SerializeField]
        private GameObject _titleReference;
        public ITextTranslator Title;
        
        [SerializeField]
        private GameObject _publishDateReference;
        public ITextTranslator PublishDate;

        private void Awake()
        {
            Title = _titleReference.GetComponent<ITextTranslator>();
            PublishDate = _publishDateReference.GetComponent<ITextTranslator>();
        }
    }
}
