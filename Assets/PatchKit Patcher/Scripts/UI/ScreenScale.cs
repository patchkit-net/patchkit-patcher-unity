using PatchKit.Unity.Patcher.Debug;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI
{
public class ScreenScale
{
    private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(ScreenScale));

    public static float Value
    {
        get
        {
            float screenScale;
            float screenDpi = Screen.dpi;
            if (screenDpi >= 384)
            {
                screenScale = 4;
            }
            else if (screenDpi >= 192)
            {
                screenScale = 2;
            }
            else
            {
                if (screenDpi == 0)
                {
                    DebugLogger.LogWarning("Unable to determine the current DPI.");
                }

                screenScale = 1;
            }

            DebugLogger.Log(string.Format("DPI: {0}", Screen.dpi));
            DebugLogger.Log(string.Format("Screen scale: {0}", screenScale));

            return screenScale;
        }
    }
}
}
