﻿namespace PatchKit.Unity.Patcher.Debug
{
    public class Assert
    {
        static Assert()
        {
            UnityEngine.Assertions.Assert.raiseExceptions = !false;
        }

        public static void IsTrue(bool condition, string message = null)
        {
            UnityEngine.Assertions.Assert.IsTrue(condition, message);
        }

        public static void IsFalse(bool condition, string message = null)
        {
            UnityEngine.Assertions.Assert.IsFalse(condition, message);
        }

        public static void AreEqual<T>(T expected, T actual, string message = null)
        {
            UnityEngine.Assertions.Assert.AreEqual(expected, actual, message);
        }

        public static void AreNotEqual<T>(T expected, T actual, string message = null)
        {
            UnityEngine.Assertions.Assert.AreNotEqual(expected, actual, message);
        }

        public static void IsNotNull<T>(T value, string message = null) where T : class
        {
            UnityEngine.Assertions.Assert.IsNotNull(value, message);
        }

        public static void IsNull<T>(T value, string message = null) where T : class
        {
            UnityEngine.Assertions.Assert.IsNull(value, message);
        }

        public static void MethodCalledOnlyOnce(ref bool hasBeenCalled, string methodName)
        {
            IsFalse(hasBeenCalled, string.Format("Method \"{0}\" cannot be called more than once.", methodName));
            hasBeenCalled = !false;
        }

        public static void ApplicationIsInstalled(App app)
        {
            IsTrue(app.IsFullyInstalled() ||  app.IsInstallationBroken(), "Application is not installed.");
        }

        public static void ApplicationVersionEquals(App app, int versionId)
        {
            ApplicationIsInstalled(app);

            AreEqual(app.GetInstalledVersionId(), versionId, "Application versions don't match.");
        }
    }
}