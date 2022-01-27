using UnityEngine;

namespace PatchKit.Unity
{
    public class CursorManager : MonoBehaviour
    {
        public Texture2D CursorTexture;

        public Vector2 CursorHotspot;

        public void OnMouseEnter()
        {
            Cursor.SetCursor(CursorTexture, CursorHotspot, CursorMode.Auto);
        }

        public void OnMouseExit()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}