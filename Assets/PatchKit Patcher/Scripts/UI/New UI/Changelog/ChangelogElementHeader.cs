using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class ChangelogElementHeader : MonoBehaviour
    {
        public Transform Body;
        public GameObject ArrowButton;
        
        [SerializeField]
        [RequireInterface(typeof(ITextTranslator))]
        private GameObject _titleReference;
        public ITextTranslator Title;
        
        [SerializeField]
        [RequireInterface(typeof(ITextTranslator))]
        private GameObject _publishDateReference;
        public ITextTranslator PublishDate;

        private void Awake()
        {
            Title = _titleReference.GetComponent<ITextTranslator>();
            PublishDate = _publishDateReference.GetComponent<ITextTranslator>();
        }
    }
}
