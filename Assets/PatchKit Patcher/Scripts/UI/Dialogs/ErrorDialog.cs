using System.Collections;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class ErrorDialog : BaseErrorDialog
    {
        public Text ErrorText;
        public TextTranslator ErrorTextTranslator;

        private void Start()
        {
            if (ErrorTextTranslator == null)
            {
                ErrorTextTranslator = ErrorText.gameObject.AddComponent<TextTranslator>();
            }
        }

        public void Confirm()
        {
            OnDisplayed();
            StartCoroutine(Retry());
        }

        public IEnumerator Retry()
        {
            while (Patcher.Instance.State.Value != PatcherState.WaitingForUserDecision)
            {
                yield return new WaitForSeconds(1);
            }

            Patcher.Instance.SetUserDecision(Patcher.UserDecision.InstallApp);
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