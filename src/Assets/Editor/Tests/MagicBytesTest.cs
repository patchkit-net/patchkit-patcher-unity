using System;
using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.Data;

public class MagicBytesTest
{
    #region Fields

    const string MacApp = "Assets/Editor/Tests/Fixtures/mac_app";
    const string WindowsApp = "Assets/Editor/Tests/Fixtures/windows_app";
    const string LinuxApp = "Assets/Editor/Tests/Fixtures/linux_app";
    
    #endregion
    
    #region Methods

    [Test]
    public void TestForMacExecutable()
    {
        PreCheck();

        Assert.IsTrue(MagicBytes.IsMacExecutable(MacApp));
        Assert.IsFalse(MagicBytes.IsMacExecutable(WindowsApp));
        Assert.IsFalse(MagicBytes.IsMacExecutable(LinuxApp));
    }

    [Test]
    public void TestForLinuxExecutable()
    {
        PreCheck();

        Assert.IsFalse(MagicBytes.IsLinuxExecutable(MacApp));
        Assert.IsFalse(MagicBytes.IsLinuxExecutable(WindowsApp));
        Assert.IsTrue(MagicBytes.IsLinuxExecutable(LinuxApp));
    }

    private static void PreCheck()
    {
        Assert.IsTrue(File.Exists(MacApp));
        Assert.IsTrue(File.Exists(WindowsApp));
        Assert.IsTrue(File.Exists(LinuxApp));
    }

    #endregion
}
