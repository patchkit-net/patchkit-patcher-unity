using PatchKit.Unity.Patcher.AppData.Local;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.Debug
{
    internal class AssertChecks
    {
        private static void Argument(bool condition, string name, string message)
        {
            Assert.IsTrue(condition, string.Format("Argument \"{0}\" {1}.", name, message));
        }

        public static void ArgumentNotNull(object value, string name)
        {
            Argument(value != null, name, "cannot be null");
        }

        public static void MethodCalledOnlyOnce(ref bool hasBeenCalled, string methodName)
        {
            Assert.IsFalse(hasBeenCalled, string.Format("Method \"{0}\" cannot be called more than once.", "ARG0"));
            hasBeenCalled = true;
        }

        public static void ApplicationIsInstalled(ILocalData localData)
        {
            Assert.IsTrue(localData.IsInstalled(), "Expected application to be installed.");
        }

        public static void ApplicationVersionEquals(ILocalData localData, int versionId)
        {
            ApplicationIsInstalled(localData);

            Assert.AreEqual(localData.GetInstalledVersion(), versionId,
                string.Format("Expected application version to equal {0}.", versionId));
        }
    }
}