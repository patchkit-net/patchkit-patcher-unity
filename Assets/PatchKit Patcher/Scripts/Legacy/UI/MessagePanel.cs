using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Legacy.UI
{
[RequireComponent(requiredComponent: typeof(Animator))]
public class MessagePanel : MonoBehaviour
{
    public Button PlayButton;
    public Button CheckButton;
    public Text CheckButtonText;

    private void Awake()
    {
        var animator = GetComponent<Animator>();
        Assert.IsNotNull(value: animator);

        Assert.IsNotNull(value: PlayButton);
        Assert.IsNotNull(value: CheckButton);
        Assert.IsNotNull(value: PlayButton.onClick);
        Assert.IsNotNull(value: CheckButton.onClick);

        PlayButton.onClick.AddListener(
            call: () => Patcher.Instance.RequestStartAppAndQuit());

        CheckButton.onClick.AddListener(
            call: () => Patcher.Instance.RequestUpdateApp());

        Patcher.Instance.OnStateChanged += state =>
        {
            Assert.IsNotNull(value: PlayButton);
            Assert.IsNotNull(value: CheckButton);
            Assert.IsNotNull(value: CheckButtonText);

            bool isOpened;

            if (state.IsInitializing ||
                state.IsQuitting ||
                state.App == null)
            {
                isOpened = false;
            }
            else
            {
                var app = state.App.Value;

                if (app.UpdateTask != null ||
                    app.IsStarting)
                {
                    isOpened = false;
                }
                else
                {
                    isOpened = true;

                    PlayButton.interactable = app.IsInstalled;

                    CheckButtonText.text = app.IsInstalled
                        ? "Check for updates"
                        : "Install";
                }
            }

            animator.SetBool(
                name: "IsOpened",
                value: isOpened);
        };
    }
}
}