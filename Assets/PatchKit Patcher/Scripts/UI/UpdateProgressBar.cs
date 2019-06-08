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
            switch (state.Kind)
            {
                case PatcherStateKind.Idle:
                    Assert.IsNotNull(value: state.AppState);

                    SetValue(
                        progress: state.AppState.InstalledVersionId.HasValue
                            ? 1f
                            : 0f);

                    break;
                case PatcherStateKind.UpdatingApp:
                    Assert.IsNotNull(value: state.AppState);

                    if (state.AppState.UpdateState.IsConnecting)
                    {
                        SetIdle(message: "Connecting...");
                    }
                    else
                    {
                        SetValue(
                            progress: (float) state.AppState.UpdateState
                                .Progress);
                    }

                    break;
                case PatcherStateKind.Initializing:
                    SetIdle(message: "Initializing...");
                    break;
                case PatcherStateKind.StartingApp:
                    SetIdle(message: "Starting...");
                    break;
                case PatcherStateKind.Quitting:
                    SetIdle(message: "Quitting...");
                    break;
                default:
                    SetIdle(message: string.Empty);
                    break;
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