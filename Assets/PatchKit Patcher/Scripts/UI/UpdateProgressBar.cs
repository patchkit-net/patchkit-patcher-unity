using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI
{
public class UpdateProgressBar : MonoBehaviour
{
    public Text Text;
    public Image Image;

    private bool _isIdle;
    private float _idleProgress = -IdleBarWidth;

    private const float IdleBarWidth = 0.2f;
    private const float IdleBarSpeed = 1.2f;

    private void Awake()
    {
        Patcher.Instance.OnStateChanged += state =>
        {
            if (state.IsInitializing)
            {
                SetIdle(message: "Initializing...");
            }
            else if (state.IsQuitting)
            {
                SetIdle(message: "Quitting...");
            }
            else if (state.App != null)
            {
                var app = state.App.Value;

                if (app.UpdateTask != null)
                {
                    var updateTask = app.UpdateTask.Value;
                    
                    if (updateTask.IsConnecting)
                    {
                        SetIdle(message: "Connecting...");
                    }
                    else
                    {
                        SetValue(progress: (float) updateTask.Progress);
                    }
                }
                else if (app.IsStarting)
                {
                    SetIdle(message: "Starting...");
                }
                else
                {
                    SetValue(progress: app.IsInstalled ? 1f : 0f);
                }
            }
            else
            {
                SetIdle(message: string.Empty);
            }
        };
    }

    private void SetImageRange(
        float from,
        float to)
    {
        Assert.IsNotNull(value: Image);
        Assert.IsNotNull(value: Image.rectTransform);

        var anchorMax = Image.rectTransform.anchorMax;
        var anchorMin = Image.rectTransform.anchorMin;

        anchorMin.x = Mathf.Clamp(
            value: from,
            min: 0f,
            max: 1f);
        anchorMax.x = Mathf.Clamp(
            value: to,
            min: 0f,
            max: 1f);

        Image.rectTransform.anchorMax = anchorMax;
        Image.rectTransform.anchorMin = anchorMin;
    }

    private void SetIdle([NotNull] string message)
    {
        Assert.IsNotNull(value: Text);

        Text.text = message;
        _isIdle = true;
    }

    private void SetValue(float progress)
    {
        Assert.IsNotNull(value: Text);
        Assert.IsNotNull(value: Image);

        _isIdle = false;
        Text.text = $"{progress * 100.0:0.0}%";
        SetImageRange(
            from: 0f,
            to: progress);
    }

    private void Update()
    {
        UpdateIdle();
    }

    private void UpdateIdle()
    {
        if (!_isIdle)
        {
            return;
        }

        SetImageRange(
            from: _idleProgress,
            to: _idleProgress + IdleBarWidth);

        _idleProgress += Time.deltaTime * IdleBarSpeed;

        if (_idleProgress >= 1f)
        {
            _idleProgress = -IdleBarWidth;
        }
    }
}
}