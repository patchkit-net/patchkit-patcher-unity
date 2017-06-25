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
            switch (error)
            {
                case PatcherError.NoInternetConnection:
                    ErrorText.text = "Please check your internet connection.";
                    break;
                case PatcherError.NoPermissions:
                    ErrorText.text = "Please check write permissions in application directory.";
                    break;
                case PatcherError.NotEnoughDiskSpace:
                    ErrorText.text = "Not enough disk space.";
                    break;
                case PatcherError.Other:
                    ErrorText.text = "Unknown error. Please try again.";
                    break;
            }
        }
    }
}
