﻿using System;
using System.IO;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using System.Text;
#endif

namespace PatchKit.Unity.Patcher.UI
{
    [RequireComponent(typeof(CanvasScaler))]
    public class BorderlessWindow : MonoBehaviour
    {
        public const string ScreenSizeFilename = "screensize";

        private PatchKit.Logging.ILogger _logger;

        public Rect DraggableArea;

        public CanvasScaler CanvasScaler;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const string UnityWindowClassName = "UnityWndClass";

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int smIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy,
            uint uFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int X;
            public int Y;

            public static implicit operator Vector2(Point point)
            {
                return new Vector2(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        private const int SwpFramechanged = 0x0020;

        private const int GwlStyle = -16;

        private const int SwMinimize = 6;

        private const int WsCaption = 0x00C00000;

        private IntPtr _windowsHandle = IntPtr.Zero;

        private static class Flags
        {
            public static void Unset<T>(ref T mask, T flag) where T : struct
            {
                int maskValue = (int) (object) mask;
                int flagValue = (int) (object) flag;

                mask = (T) (object) (maskValue & (~flagValue));
            }
        }

        private static Vector2 GetCursorPosition()
        {
            Point lpPoint;
            GetCursorPos(out lpPoint);

            return lpPoint;
        }

        private Vector2 GetScreenResoultion()
        {
            Vector2 r = new Vector2();

            r.x = GetSystemMetrics(78);
            r.y = GetSystemMetrics(79);

            return r;
        }

        private Rect _windowRect;

        private bool _isDragged;

        private Vector2 _dragStartPosition;

        private Vector2 _dragWindowStartPosition;
#endif

        private void Awake()
        {
            _logger = PatcherLogManager.DefaultLogger;
            if (CanvasScaler == null)
            {
                CanvasScaler = GetComponent<CanvasScaler>();
            }

            float screenScale;
            float screenDpi = Screen.dpi;
            if (screenDpi >= 400)
            {
                screenScale = 4;
            }
            else if (screenDpi >= 200)
            {
                screenScale = 2;
            }
            else
            {
                if (screenDpi == 0)
                {
                    _logger.LogWarning("Unable to determine the current DPI.");
                }

                screenScale = 1;
            }

            _logger.LogDebug(string.Format("Screen scale: {0}", screenScale));

            EnforceCorrectScreenSize(screenScale);

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            DraggableArea.width = DraggableArea.width * screenScale;
            DraggableArea.height = DraggableArea.height * screenScale;
            DraggableArea.x = DraggableArea.x * screenScale;
            DraggableArea.y = DraggableArea.y * screenScale;

            _windowRect.position = new Vector2(Screen.currentResolution.width/2.0f - Screen.width/2.0f,
                Screen.currentResolution.height/2.0f - Screen.height/2.0f);
            _windowRect.size = new Vector2(Screen.width, Screen.height);

            Application.runInBackground = true;

            uint threadId = GetCurrentThreadId();
            EnumThreadWindows(threadId, (hWnd, lParam) =>
            {
                var classText = new StringBuilder(UnityWindowClassName.Length + 1);
                GetClassName(hWnd, classText, classText.Capacity);
                if (classText.ToString() == UnityWindowClassName)
                {
                    _windowsHandle = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
#endif
        }

        private void EnforceCorrectScreenSize(float screenScale)
        {
            string screenSizeFilePath = Path.Combine(Application.dataPath, ScreenSizeFilename);
            _logger.LogDebug("Reading correct screen size from " + screenSizeFilePath);

            if (!File.Exists(screenSizeFilePath))
            {
                _logger.LogWarning(screenSizeFilePath + " file does not exist.");
                return;
            }

            var screenResolutionText = File.ReadAllText(screenSizeFilePath).Split(' ');

            try
            {
                int width = (int) (int.Parse(screenResolutionText[0]) * screenScale);
                int height = (int) (int.Parse(screenResolutionText[1]) * screenScale);

                PlayerPrefs.SetInt("Screenmanager Resolution Width", width);
                PlayerPrefs.SetInt("Screenmanager Resolution Height", height);
                PlayerPrefs.SetInt("Screenmanager Is Fullscreen mode", 0);

                Screen.SetResolution(width, height, false);
                CanvasScaler.scaleFactor = screenScale;
            }
            catch (System.Exception e)
            {
                _logger.LogError("Failed to correct screen sizing due to an exception.", e);
            }
        }

        private void Update()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (Input.GetMouseButton(0) && _isDragged)
            {
                Vector2 translation = _dragStartPosition - GetCursorPosition();

                _windowRect.position = _dragWindowStartPosition - translation;
            }
            else if (Input.GetMouseButtonDown(0) && DraggableArea.Contains(Input.mousePosition))
            {
                _dragStartPosition = GetCursorPosition();
                _dragWindowStartPosition = _windowRect.position;
                _isDragged = true;
            }
            else
            {
                _isDragged = false;
                _windowRect.size = new Vector2(Screen.width, Screen.height);
            }

            Vector2 screenMax = GetScreenResoultion();

            screenMax.x -= Screen.width;
            screenMax.y -= Screen.height;

            _windowRect.position = new Vector2(Mathf.Clamp(_windowRect.position.x, 0.0f, screenMax.x),
                Mathf.Clamp(_windowRect.position.y, 0.0f, screenMax.y));

            int flags = (int) GetWindowLongPtr(_windowsHandle, GwlStyle);

            Flags.Unset(ref flags, WsCaption);

            SetWindowLongPtr(_windowsHandle, GwlStyle, flags);

            SetWindowPos(_windowsHandle, -2, (int) _windowRect.x, (int) _windowRect.y, (int) _windowRect.width,
                (int) _windowRect.height, SwpFramechanged);

            SetWindowLongPtr(_windowsHandle, GwlStyle, flags);

            SetWindowLongPtr(_windowsHandle, GwlStyle, flags);
#endif
        }

        public void MinimizeWindow()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            ShowWindow(_windowsHandle, SwMinimize);
#endif
        }

        public void CloseWindow()
        {
            Application.Quit();
        }
    }
}
