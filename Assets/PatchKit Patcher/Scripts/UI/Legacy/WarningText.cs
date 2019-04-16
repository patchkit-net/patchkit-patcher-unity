using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI.Legacy
{
public class WarningText : MonoBehaviour
{
    public Text Text;

    private void Awake()
    {
        Assert.IsNotNull(value: Text);

        Text.text = string.Empty;
    }
}
}