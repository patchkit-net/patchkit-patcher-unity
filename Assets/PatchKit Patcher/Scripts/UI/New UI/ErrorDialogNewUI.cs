using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class ErrorDialogNewUI : AErrorDialog
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
            Patcher.Instance.SetUserDecision(Patcher.UserDecision.CheckForAppUpdates);
            OnDisplayed();
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