using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.Data;

public class MagicBytesTest
{
    #region Methods

    [Test]
    public void TestForMacExecutable()
    {
        const string macApp = "Assets/Editor/Tests/Fixtures/mac_app";
        const string windowsApp = "Assets/Editor/Tests/Fixtures/windows_app";

        Assert.IsTrue(File.Exists(macApp));
        Assert.IsTrue(File.Exists(windowsApp));

        Assert.IsTrue(MagicBytes.IsMacExecutable(macApp));
        Assert.IsFalse(MagicBytes.IsMacExecutable(windowsApp));
    }
    #endregion
}
