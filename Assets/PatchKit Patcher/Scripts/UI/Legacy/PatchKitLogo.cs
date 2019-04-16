using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UI.Legacy
{
[RequireComponent(requiredComponent: typeof(Button))]
[RequireComponent(requiredComponent: typeof(Image))]
public class PatchKitLogo : MonoBehaviour
{
    public Texture2D CursorTexture;
    public Vector2 CursorHotspot;

    private const string PatchKitWebsiteUrl =
        "https://patchkit.net/?source=patcher";

    private void Awake()
    {
        Assert.IsNotNull(value: CursorTexture);

        var button = GetComponent<Button>();
        var image = GetComponent<Image>();

        Assert.IsNotNull(value: button);
        Assert.IsNotNull(value: image);

        button.enabled = false;
        image.enabled = false;

        Patcher.Instance.StateChanged += state =>
        {
            Assert.IsNotNull(value: state);

            bool isWhitelabel = state.AppState.Info?.PatcherWhitelabel ?? true;

            button.enabled = !isWhitelabel;
            image.enabled = !isWhitelabel;
        };
    }

    public void OnMouseEnter()
    {
        Cursor.SetCursor(
            texture: CursorTexture,
            hotspot: CursorHotspot,
            cursorMode: CursorMode.Auto);
    }

    public void OnMouseExit()
    {
        Cursor.SetCursor(
            texture: null,
            hotspot: Vector2.zero,
            cursorMode: CursorMode.Auto);
    }

    public void GoToPatchKit()
    {
        Application.OpenURL(url: PatchKitWebsiteUrl);
    }
}
}