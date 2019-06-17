using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Utilities;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class ErrorDialog : Dialog<ErrorDialog>
    {
        public Text ErrorText;

        public void Confirm()
        {
            OnDisplayed();
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
