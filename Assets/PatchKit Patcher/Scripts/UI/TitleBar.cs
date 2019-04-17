using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI
{
public class TitleBar : MonoBehaviour
{
    public Button CloseButton;
    public Button MinimizeButton;

    private void Awake()
    {
        Assert.IsNotNull(value: CloseButton);
        Assert.IsNotNull(value: MinimizeButton);

        bool isEnabled = Application.platform == RuntimePlatform.WindowsPlayer;

        CloseButton.gameObject.SetActive(value: isEnabled);
        MinimizeButton.gameObject.SetActive(value: isEnabled);
    }
}
}