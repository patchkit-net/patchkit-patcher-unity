using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Legacy.UI
{
public class GameTitle : MonoBehaviour
{
    public Text Text;

    private void Awake()
    {
        Assert.IsNotNull(value: Text);

        Patcher.Instance.StateChanged += state =>
        {
            Assert.IsNotNull(value: state);

            Text.text = state.AppState.Info?.DisplayName ?? "GAME";
        };
    }
}
}