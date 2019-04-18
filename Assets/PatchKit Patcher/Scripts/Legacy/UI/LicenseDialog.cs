using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Legacy.UI
{
public class LicenseDialog : MonoBehaviour
{
    public Text ErrorMessageText;
    public InputField KeyInputField;

    [Multiline]
    public string InvalidLicenseMessageText;

    [Multiline]
    public string BlockedLicenseMessageText;

    //TODO: Use it
    [Multiline]
    public string ServiceUnavailableMessageText;

    private void Awake()
    {
        var animator = GetComponent<Animator>();

        Assert.IsNotNull(value: animator);

        Patcher.Instance.StateChanged += state =>
        {
            Assert.IsNotNull(value: state);
            Assert.IsNotNull(value: ErrorMessageText);

            bool isAskingForLicenseKey =
                state.Kind == PatcherStateKind.AskingForAppLicenseKey;

            animator.SetBool(
                name: "IsOpened",
                value: isAskingForLicenseKey);

            if (!isAskingForLicenseKey)
            {
                return;
            }

            Assert.IsNotNull(value: state.AppState);

            switch (state.AppState.LicenseKeyIssue)
            {
                case PatcherAppLicenseKeyIssue.None:
                    ErrorMessageText.text = string.Empty;
                    break;
                case PatcherAppLicenseKeyIssue.Invalid:
                    ErrorMessageText.text = InvalidLicenseMessageText;
                    break;
                case PatcherAppLicenseKeyIssue.Blocked:
                    ErrorMessageText.text = BlockedLicenseMessageText;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };
    }

    public void Confirm()
    {
        Assert.IsNotNull(value: KeyInputField);

        string licenseKey = KeyInputField.text;

        if (string.IsNullOrEmpty(value: licenseKey))
        {
            return;
        }

        licenseKey = licenseKey.ToUpper().Trim();

        Patcher.Instance.OnUpdateAppWithLicenseKeyRequested(
            licenseKey: licenseKey);
    }

    public void Abort()
    {
        Patcher.Instance.OnCancelUpdateAppRequested();
    }
}
}