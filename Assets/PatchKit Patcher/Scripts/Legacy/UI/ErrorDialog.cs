using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Legacy.UI
{
public class ErrorDialog : MonoBehaviour
{
    public Text ErrorText;

    private void Awake()
    {
        var animator = GetComponent<Animator>();

        Assert.IsNotNull(value: animator);

        Patcher.Instance.StateChanged += state =>
        {
            Assert.IsNotNull(value: state);
            Assert.IsNotNull(value: ErrorText);

            bool isOpened = false;

            switch (state.Kind)
            {
                case PatcherStateKind.DisplayingNoLauncherError:
                    isOpened = true;
                    ErrorText.text = "Patcher must be started with launcher.";
                    break;
                case PatcherStateKind.DisplayingMultipleInstancesError:
                    isOpened = true;
                    ErrorText.text =
                        "Another instance of patcher is already running.";
                    break;
                case PatcherStateKind.DisplyingOutOfDiskSpaceError:
                    isOpened = true;
                    ErrorText.text =
                        "You don't have enough disk space to install the application.\n" +
                        "Please make some and restart the installation.";
                    break;
                case PatcherStateKind.DisplayingInternalError:
                    isOpened = true;
                    ErrorText.text = "Internal error.";
                    break;
                case PatcherStateKind.DisplayingUnauthorizedAccessError:
                    isOpened = true;
                    ErrorText.text =
                        "Patcher don't have enough permissions to install the application.";
                    break;
                default:
                    ErrorText.text = string.Empty;
                    break;
            }

            animator.SetBool(
                name: "IsOpened",
                value: isOpened);
        };
    }

    public void Confirm()
    {
        Patcher.Instance.OnAcceptErrorRequested();
    }
}
}