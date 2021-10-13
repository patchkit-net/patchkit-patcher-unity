using System.Collections;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class ErrorDialog : Dialog<ErrorDialog>
    {
        public Text ErrorText;

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

        public void Display(PatcherError error, CancellationToken cancellationToken)
        {
            UnityDispatcher.Invoke(() => UpdateMessage(error)).WaitOne();

            Display(cancellationToken);
        }

        private void UpdateMessage(PatcherError error)
        {
            ErrorText.text = error.ToString();
        }
    }
}
