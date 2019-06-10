using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Legacy.UI
{
public class ErrorDialog : MonoBehaviour
{
    public Text ErrorText;

    private Error? _error;

    private Animator _animator;
    private static readonly int AnimatorIsOpened = Animator.StringToHash("IsOpened");

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        Assert.IsNotNull(value: _animator);

        Patcher.Instance.OnError += error =>
        {
            _error = error;

            _animator.SetBool(
                id: AnimatorIsOpened,
                value: true);

            //TODO: More descriptive messages, like:
            // - if issue persists, please contact support
            // - patcher will close, please start it again
            switch (error)
            {
                case Error.CriticalError:
                    ErrorText.text = "Critical error.";
                    break;
                case Error.StartedWithoutLauncher:
                    ErrorText.text = "Patcher must be started with launcher.";
                    break;
                case Error.MultipleInstances:
                    ErrorText.text =
                        "Another instance of patcher is already running.";
                    break;
                case Error.AppDataUnauthorizedAccess:
                    ErrorText.text =
                        "Patcher don't have enough permissions to install the application.";
                    break;
                case Error.UpdateAppRunOutOfFreeDiskSpace:
                    ErrorText.text =
                        "You don't have enough disk space to install the application.\n" +
                        "Please make some and restart the installation.";
                    break;
                case Error.UpdateAppError:
                    ErrorText.text = "Updating app has failed."; //TODO: Would you like to retry?
                    break;
                case Error.StartAppError:
                    ErrorText.text = "Starting app has failed."; //TODO: Would you like to retry?
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };
    }

    public void Confirm()
    {
        if (_error == null)
        {
            return;
        }

        _animator.SetBool(
            id: AnimatorIsOpened,
            value: false);

        switch (_error.Value)
        {
            case Error.CriticalError:
                Patcher.Instance.RequestQuit();
                break;
            case Error.StartedWithoutLauncher:
                Patcher.Instance.RequestRestartWithLauncher();
                break;
            case Error.MultipleInstances:
                Patcher.Instance.RequestQuit();
                break;
            case Error.AppDataUnauthorizedAccess:
                Patcher.Instance.RequestRestartWithHigherPermissions();
                break;
            case Error.UpdateAppRunOutOfFreeDiskSpace:
                break;
            case Error.UpdateAppError:
                break;
            case Error.StartAppError:
                break;
        }

        _error = null;
    }
}
}