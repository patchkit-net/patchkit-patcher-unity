﻿using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class LastRelease : MonoBehaviour
    {
        [SerializeField]
        private GameObject _publishDateReference;
        public ITextTranslator PublishDate;
        
        [SerializeField]
        private GameObject _labelReference;
        public ITextTranslator Label;
        
        public Transform ChangeList;
        
        private void Awake()
        {
            Label = _labelReference.GetComponent<ITextTranslator>();
            PublishDate = _publishDateReference.GetComponent<ITextTranslator>();
        }
    }
}