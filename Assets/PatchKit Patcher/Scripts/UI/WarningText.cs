using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class WarningText : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            Patcher.Instance.Warning.ObserveOnMainThread().Subscribe(warning =>
            {
                Text.text = warning;
            }).AddTo(this);
        }
    }
}