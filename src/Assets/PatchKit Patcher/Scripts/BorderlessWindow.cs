using UnityEngine;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using System.Text;
#endif

namespace PatchKit.Unity.Patcher
{
    public class BorderlessWindow : MonoBehaviour
    {
        public Rect DraggableArea;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private const string UnityWindowClassName = "UnityWndClass";

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int smIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy,
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
        private struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Vector2(POINT point)
            {
                return new Vector2(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private const int
            SWP_FRAMECHANGED = 0x0020,
            SWP_NOMOVE = 0x0002,
            SWP_NOSIZE = 0x0001,
            SWP_NOZORDER = 0x0004,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_SHOWWINDOW = 0x0040,
            SWP_NOSENDCHANGING = 0x0400;

        private const int GWL_STYLE = -16;

        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;

        private const int
            WS_BORDER = 0x00800000,
            WS_CAPTION = 0x00C00000,
            WS_CHILD = 0x40000000,
            WS_CHILDWINDOW = 0x40000000,
            WS_CLIPCHILDREN = 0x02000000,
            WS_CLIPSIBLINGS = 0x04000000,
            WS_DISABLED = 0x08000000,
            WS_DLGFRAME = 0x00400000,
            WS_GROUP = 0x00020000,
            WS_HSCROLL = 0x00100000,
            WS_ICONIC = 0x20000000,
            WS_MAXIMIZE = 0x01000000,
            WS_MAXIMIZEBOX = 0x00010000,
            WS_MINIMIZE = 0x20000000,
            WS_MINIMIZEBOX = 0x00020000,
            WS_OVERLAPPED = 0x00000000,
            WS_OVERLAPPEDWINDOW =
                WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
            WS_POPUP = unchecked((int) 0x80000000),
            WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
            WS_SIZEBOX = 0x00040000,
            WS_SYSMENU = 0x00080000,
            WS_TABSTOP = 0x00010000,
            WS_THICKFRAME = 0x00040000,
            WS_TILED = 0x00000000,
            WS_TILEDWINDOW =
                WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
            WS_VISIBLE = 0x10000000,
            WS_VSCROLL = 0x00200000;

        private IntPtr _WINDOWS_HANDLE_ = IntPtr.Zero;

        private static class Flags
        {
            public static void Set<T>(ref T mask, T flag) where T : struct
            {
                int maskValue = (int) (object) mask;
                int flagValue = (int) (object) flag;

                mask = (T) (object) (maskValue | flagValue);
            }

            public static void Unset<T>(ref T mask, T flag) where T : struct
            {
                int maskValue = (int) (object) mask;
                int flagValue = (int) (object) flag;

                mask = (T) (object) (maskValue & (~flagValue));
            }

            public static void Toggle<T>(ref T mask, T flag) where T : struct
            {
                if (Contains(mask, flag))
                {
                    Unset(ref mask, flag);
                }
                else
                {
                    Set(ref mask, flag);
                }
            }

            public static bool Contains<T>(T mask, T flag) where T : struct
            {
                return Contains((int) (object) mask, (int) (object) flag);
            }

            public static bool Contains(int mask, int flag)
            {
                return (mask & flag) != 0;
            }
        }

        private static Vector2 GetCursorPosition()
        {
            POINT lpPoint;
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
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            _windowRect.position = new Vector2(Screen.currentResolution.width/2.0f - Screen.width/2.0f,
                Screen.currentResolution.height/2.0f - Screen.height/2.0f);
            _windowRect.size = new Vector2(Screen.width, Screen.height);

            UnityEngine.Application.runInBackground = true;

            uint threadId = GetCurrentThreadId();
            EnumThreadWindows(threadId, (hWnd, lParam) =>
            {
                var classText = new StringBuilder(UnityWindowClassName.Length + 1);
                GetClassName(hWnd, classText, classText.Capacity);
                if (classText.ToString() == UnityWindowClassName)
                {
                    _WINDOWS_HANDLE_ = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
#endif
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

            int flags = (int) GetWindowLongPtr(_WINDOWS_HANDLE_, GWL_STYLE);

            Flags.Unset(ref flags, WS_CAPTION);

            SetWindowLongPtr(_WINDOWS_HANDLE_, GWL_STYLE, flags);

            SetWindowPos(_WINDOWS_HANDLE_, -2, (int) _windowRect.x, (int) _windowRect.y, (int) _windowRect.width,
                (int) _windowRect.height, SWP_FRAMECHANGED);

            SetWindowLongPtr(_WINDOWS_HANDLE_, GWL_STYLE, flags);

            SetWindowLongPtr(_WINDOWS_HANDLE_, GWL_STYLE, flags);
#endif
        }

        public void MinimizeWindow()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            ShowWindow(_WINDOWS_HANDLE_, SW_MINIMIZE);
#endif
        }

        public void CloseWindow()
        {
            UnityEngine.Application.Quit();
        }
    }
}