﻿using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.UI.Languages;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class DiscFreeSpace : MonoBehaviour
    {
        public TextMeshProTranslator sizeTextMeshProTranslator;

        void Start()
        {
            var text = AvailableDiskSpace.Instance.FreeDiskSpace.Select(
                freeDiscSpace => string.Format("{0:0.0}MB", freeDiscSpace / 1024.0 / 1024.0));

            text.ObserveOnMainThread().Subscribe(t => sizeTextMeshProTranslator.SetText(t)).AddTo(this);
        }
    }
}