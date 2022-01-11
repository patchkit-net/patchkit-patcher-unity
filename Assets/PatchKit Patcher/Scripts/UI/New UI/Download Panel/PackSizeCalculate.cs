using System;
using PatchKit.Unity.UI.Languages;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class PackSizeCalculate : MonoBehaviour
    {
        public TextMeshProTranslator sizeTextMeshProTranslator;
        
        void Start()
        {
            var text = Patcher.Instance.SizeLastContentSummary.Select(
                sizeLastContentSummary =>
                {
                    if (sizeLastContentSummary != 0)
                    {
                        return string.Format("{0:0.0}MB", sizeLastContentSummary / 1024.0 / 1024.0);
                    }
                    return String.Empty;
                });

            text.ObserveOnMainThread().Subscribe(t => sizeTextMeshProTranslator.SetText(t)).AddTo(this);
        }
    }
}