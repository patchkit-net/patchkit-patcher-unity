﻿using System.Collections;
using System.Collections.Generic;
using PatchKit.Unity.UI;
using PatchKit.Unity.Utilities;
using TMPro;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class LastRelease : MonoBehaviour
    {
        [SerializeField]
        [RequireInterface(typeof(ITextTranslator))]
        private GameObject _publishDateReference;
        public ITextTranslator PublishDate;
        
        [SerializeField]
        [RequireInterface(typeof(ITextTranslator))]
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