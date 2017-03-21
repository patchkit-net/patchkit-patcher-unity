using System.IO;
using NSubstitute;
using NUnit.Framework;
using PatchKit.Unity.Patcher;
using PatchKit.Unity.Utilities;
using UnityEngine;

public class AppFinderTest
{

    private string _tempDir;

    [SetUp]
    public void SetUp()
    {
        _tempDir = TestHelpers.CreateTemporaryDirectory();
    }

    [TearDown]
    public void TearDown()
    {
        // revert changes to Platform
        Platform.PlatformResolver = new PlatformResolver();

        TestHelpers.DeleteTemporaryDirectory(_tempDir);
    }

    [Test]
    public void FindLinuxApp()
    {
        var platformResolver = Substitute.For<PlatformResolver>();
        platformResolver.GetRuntimePlatform().Returns(RuntimePlatform.LinuxPlayer);

        // empty directory should be ignored
        Directory.CreateDirectory(Path.Combine(_tempDir, "directory"));

        // copy linux app
        string source = TestFixtures.GetFilePath("magicbytes-test/linux_app");
        string dest = Path.Combine(_tempDir, "executable");

        File.Copy(source, dest);

        var appFinder = new AppFinder();
        string executable = appFinder.FindLinuxExecutable(_tempDir);
        Assert.AreEqual(dest, executable);
    }
}
