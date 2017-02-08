using NUnit.Framework;
using PatchKit.Unity.Patcher.Data;

public class MagicBytesTest
{
    private readonly string _macApp = TestFixtures.GetFilePath("magicbytes-test/mac_app");
    private readonly string _windowsApp = TestFixtures.GetFilePath("magicbytes-test/windows_app");
    private readonly string _linuxApp = TestFixtures.GetFilePath("magicbytes-test/linux_app");

    [Test]
    public void IsMacExecutable_ForMacApp_ReturnsTrue()
    {
        Assert.IsTrue(MagicBytes.IsMacExecutable(_macApp));
    }

    [Test]
    public void IsMacExecutable_ForWindowsApp_ReturnsFalse()
    {
        Assert.IsFalse(MagicBytes.IsMacExecutable(_windowsApp));
    }

    [Test]
    public void IsMacExecutable_ForLinuxApp_ReturnsFalse()
    {
        Assert.IsFalse(MagicBytes.IsMacExecutable(_linuxApp));
    }

    [Test]
    public void IsLinuxExecutable_ForMacApp_ReturnsFalse()
    {
        Assert.IsFalse(MagicBytes.IsLinuxExecutable(_macApp));
    }

    [Test]
    public void IsLinuxExecutable_ForWindowsApp_ReturnsFalse()
    {
        Assert.IsFalse(MagicBytes.IsLinuxExecutable(_windowsApp));
    }

    [Test]
    public void IsLinuxExecutable_ForLinuxApp_ReturnsTrue()
    {
        Assert.IsTrue(MagicBytes.IsLinuxExecutable(_linuxApp));
    }
}