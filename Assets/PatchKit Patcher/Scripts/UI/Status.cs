using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI
{
public class Status : MonoBehaviour
{
    public Text Text;

    private void Awake()
    {
        Patcher.Instance.OnStateChanged += state =>
        {
            Assert.IsNotNull(value: Text);

            if (state.IsInitializing)
            {
                Text.text = "Initializing...";
            }
            else if (state.IsQuitting)
            {
                Text.text = "Quitting...";
            }
            else if (state.App != null)
            {
                if (state.App.Value.UpdateTask != null)
                {
                    Text.text = "Updating...";
                }
                else if (state.App.Value.IsStarting)
                {
                    Text.text = "Starting...";
                }
                else
                {
                    Text.text = string.Empty;
                }
            }
            else
            {
                Text.text = string.Empty;
            }
        };
    }
}
}