using System;
using PatchKit.Unity.UI.Languages;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class PackSizeCalculate : MonoBehaviour
    {
        public TextTranslator sizeTextTranslator;

        void Start()
        {
            IObservable<string> text = Patcher.Instance.SizeLastContentSummary.Select(
                sizeLastContentSummary => sizeLastContentSummary != 0
                    ? string.Format("{0:0.0}MB", sizeLastContentSummary / 1024.0 / 1024.0)
                    : string.Empty);

            text.ObserveOnMainThread().Subscribe(t => sizeTextTranslator.SetText(t)).AddTo(this);
        }
    }
}