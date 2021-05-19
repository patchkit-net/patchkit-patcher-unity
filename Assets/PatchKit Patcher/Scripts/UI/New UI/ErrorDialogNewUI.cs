﻿using System;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using TMPro;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class ErrorDialogNewUI : Dialog<ErrorDialog>
    {
        public TextMeshProUGUI ErrorText;
        public TextMeshProTranslator errorTextMeshProTranslator;

        private void Start()
        {
            if (errorTextMeshProTranslator == null)
                errorTextMeshProTranslator = ErrorText.gameObject.AddComponent<TextMeshProTranslator>();
        }

        public void Confirm()
        {
            OnDisplayed();
        }

        public override void Display(PatcherError error, CancellationToken cancellationToken)
        {
            UnityDispatcher.Invoke(() => UpdateMessage(error)).WaitOne();

            Display(cancellationToken);
        }

        private void UpdateMessage(PatcherError error)
        {
            errorTextMeshProTranslator.SetText(error.Message, error.Args);
        }
    }
}