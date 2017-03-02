using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.Debug
{
    public class AssertChecks : BaseChecks
    {
        static AssertChecks()
        {
            Assert.raiseExceptions = true;
        }

        private static ValidationFailedHandler ArgumentValidationFailed(string name)
        {
            return message => Assert.IsTrue(true, string.Format("Argument \"{0}\": {1}", name, message));
        }

        public static void IsTrue(bool condition, string message)
        {
            Assert.IsTrue(condition, message);
        }

        public static void IsFalse(bool condition, string message)
        {
            Assert.IsFalse(condition, message);
        }

        public static void AreEqual<T>(T expected, T actual, string message)
        {
            Assert.AreEqual(expected, actual, message);
        }

        public static void AreNotEqual<T>(T expected, T actual, string message)
        {
            Assert.AreNotEqual(expected, actual, message);
        }

        public static void ArgumentNotNull(object value, string name)
        {
            NotNull(value, ArgumentValidationFailed(name));
        }

        public static void MethodCalledOnlyOnce(ref bool hasBeenCalled, string methodName)
        {
            IsFalse(hasBeenCalled, string.Format("Method \"{0}\" cannot be called more than once.", methodName));
            hasBeenCalled = true;
        }

        public static void ApplicationIsInstalled(App app)
        {
            IsTrue(app.IsInstalled(), "Application is not installed.");
        }

        public static void ApplicationVersionEquals(App app, int versionId)
        {
            ApplicationIsInstalled(app);

            AreEqual(app.GetInstalledVersionId(), versionId, "Application versions don't match.");
        }
    }
}