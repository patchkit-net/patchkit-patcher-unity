using System;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class ErrorDialog : AErrorDialog
    {
        public Text ErrorText;
        public TextTranslator ErrorTextTranslator;

        private void Start()
        {
            if (ErrorTextTranslator == null)
                ErrorTextTranslator = ErrorText.gameObject.AddComponent<TextTranslator>();
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
            ErrorTextTranslator.SetText(error.Message, error.Args);
        }
    }
}