using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UniRx;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity
{

    public class PatchKitLogo : MonoBehaviour
    {
        [SerializeField]
        private string patchKitWebsiteUrl;

        [SerializeField]
        private Texture2D cursorTexture;

        [SerializeField]
        private Vector2 cursorHotspot;

        [SerializeField]
        private Image image;

        [SerializeField]
        private Button button;

        private bool isLogoVisible = false;

        private const float LogoVisibilityChangeDelay = 3; // in seconds

        void Awake()
        {
            Assert.IsNotNull(button);
            Assert.IsNotNull(image);

            Assert.IsNotNull(cursorTexture);

            button.enabled = false;
            image.enabled = false;
        }

        void Start()
        {
            var patcher = Patcher.Patcher.Instance;

            Assert.IsNotNull(patcher);

            patcher.AppInfo
                .ObserveOnMainThread()
                .Subscribe(app => Resolve(app.PatcherWhitelabel))
                .AddTo(this);

            StartCoroutine(ChangeVisibility());
        }

        private IEnumerator ChangeVisibility()
        {
            while (true)
            {
                yield return new WaitForSeconds(LogoVisibilityChangeDelay);
                image.enabled = isLogoVisible;
                button.enabled = isLogoVisible;
            }
        }

        private void Resolve(bool isWhitelabel)
        {
            isLogoVisible = !isWhitelabel;
        }

        public void OnMouseEnter()
        {
            Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
        }

        public void OnMouseExit()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        public void GoToPatchKit()
        {
            Application.OpenURL(patchKitWebsiteUrl);
        }
    }
}