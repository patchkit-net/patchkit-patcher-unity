using System;
using UnityEngine;

namespace PatchKit.Unity.Utilities
{
    [Serializable]
    public class PlatformDependentValue<T>
    {
        public T Windows;

        public T MacOSX;

        public T Linux;

        public static implicit operator T(PlatformDependentValue<T> value)
        {
            return value.GetValue();
        }

        /// <summary>
        /// Returns the value respective to current platform.
        /// </summary>
        public T GetValue()
        {
            if (Application.platform == RuntimePlatform.OSXDashboardPlayer ||
                Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                return MacOSX;
            }

            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                return Windows;
            }

            if (Application.platform == RuntimePlatform.LinuxPlayer)
            {
                return Linux;
            }

            throw new InvalidOperationException("PlatformDependentValue class supports only Standalone platforms.");
        }
    }

    [Serializable]
    public class PlatformDependentString : PlatformDependentValue<string>
    {
    }

    [Serializable]
    public class PlatformDependentInt : PlatformDependentValue<int>
    {
    }

    [Serializable]
    public class PlatformDependentFloat : PlatformDependentValue<float>
    {
    }
}
