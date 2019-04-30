using System;
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

            bool isOpened = state.Kind == PatcherStateKind.DisplayingError;

            if (!isOpened)
            {
                animator.SetBool(
                    name: "IsOpened",
                    value: false);

                return;
            }

            animator.SetBool(
                name: "IsOpened",
                value: true);

            Assert.IsTrue(condition: state.Error.HasValue);

            switch (state.Error.Value)
            {
                case PatcherError.NoLauncherError:
                    ErrorText.text = "Patcher must be started with launcher.";
                    break;
                case PatcherError.MultipleInstancesError:
                    ErrorText.text =
                        "Another instance of patcher is already running.";
                    break;
                case PatcherError.OutOfDiskSpaceError:
                    ErrorText.text =
                        "You don't have enough disk space to install the application.\n" +
                        "Please make some and restart the installation.";
                    break;
                case PatcherError.InternalError:
                    ErrorText.text = "Internal error.";
                    break;
                case PatcherError.UnauthorizedAccessError:
                    ErrorText.text =
                        "Patcher don't have enough permissions to install the application.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };
    }

    public void Confirm()
    {
        Patcher.Instance.OnAcceptErrorRequested();
    }
}
}