﻿using PatchKit.Unity.UI.Languages;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class DownloadStarts : MonoBehaviour
    {
        private ITextTranslator _textTranslator;

        private void Start()
        {
            _textTranslator = GetComponent<ITextTranslator>() ?? gameObject.AddComponent<TextTranslator>();

            Patcher.Instance.State.ObserveOnMainThread().Subscribe(state =>
            {
                if (state == PatcherState.UpdatingApp)
                {
                    _textTranslator.SetText(System.DateTime.Now.ToString("HH:mm"));
                }
            }).AddTo(this);
        }
    }
}