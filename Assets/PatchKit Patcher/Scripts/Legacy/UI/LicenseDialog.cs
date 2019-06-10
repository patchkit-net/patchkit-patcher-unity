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

    private Animator _animator;
    private static readonly int AnimatorIsOpened = Animator.StringToHash("IsOpened");

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        Assert.IsNotNull(value: _animator);

        Patcher.Instance.OnAppLicenseKeyIssue += (
            licenseKey,
            issue) =>
        {
            Assert.IsNotNull(value: KeyInputField);
            Assert.IsNotNull(value: ErrorMessageText);

            _animator.SetBool(
                id: AnimatorIsOpened,
                value: true);

            KeyInputField.text = licenseKey ?? string.Empty;

            switch (issue)
            {
                case AppLicenseKeyIssue.None:
                    ErrorMessageText.text = string.Empty;
                    break;
                case AppLicenseKeyIssue.Invalid:
                    ErrorMessageText.text = InvalidLicenseMessageText;
                    break;
                case AppLicenseKeyIssue.Blocked:
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

        Patcher.Instance.RequestSetAppLicenseKeyAndUpdateApp(
            licenseKey: licenseKey);

        _animator.SetBool(
            id: AnimatorIsOpened,
            value: false);
    }

    public void Abort()
    {
        _animator.SetBool(
            id: AnimatorIsOpened,
            value: false);
    }
}
}