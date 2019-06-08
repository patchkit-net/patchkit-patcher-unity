using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI
{
public class AppName : MonoBehaviour
{
    public Text Text;

    private void Awake()
    {
        Assert.IsNotNull(value: Text);

        Patcher.Instance.OnStateChanged += state =>
        {
            Text.text = state.App?.Info?.DisplayName ?? "APPLICATION";
        };
    }
}
}