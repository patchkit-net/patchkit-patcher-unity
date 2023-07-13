using System.Collections;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class ErrorDialogNewUI : BaseErrorDialog
    {
        public Text ErrorText;
        public TextTranslator errorTextTranslator;

        private void Start()
        {
            if (errorTextTranslator == null)
                errorTextTranslator = ErrorText.gameObject.AddComponent<TextTranslator>();
        }

        public void Wait()
        {
            OnDisplayed();
        }
        
        public void Restart()
        {
            StartCoroutine(Retry());
            OnDisplayed();
        }
        
        public IEnumerator Retry()
        {
            while (Patcher.Instance.State.Value != PatcherState.WaitingForUserDecision)
            {
                yield return new WaitForSeconds(1);
            }
            Patcher.Instance.SetUserDecision(Patcher.UserDecision.CheckForAppUpdates);
        }

        public override void Display(PatcherError error, CancellationToken cancellationToken)
        {
            UnityDispatcher.Invoke(() => UpdateMessage(error)).WaitOne();

            Display(cancellationToken);
        }

        private void UpdateMessage(PatcherError error)
        {
            errorTextTranslator.SetText(error.Message, error.Args);
        }
    }
}