#if UNITY_2018
using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;

using EnvironmentVariables = PatchKit.Unity.Patcher.Debug.EnvironmentVariables;

class TemporaryDirectoryTest
{
    private string _dirPath;

    [SetUp]
    public void Setup()
    {
        _dirPath = TestHelpers.CreateTemporaryDirectory();
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.DeleteTemporaryDirectory(_dirPath);
    }

    [Test]
    public void Constructor_CreatesDirectory()
    {
        TemporaryDirectory.ExecuteIn(_dirPath, dir => {
            Assert.IsTrue(Directory.Exists(_dirPath));
        });
    }

    [Test]
    public void Dispose_DeletesDirectory()
    {
        TemporaryDirectory.ExecuteIn(_dirPath, dir => {});

        Assert.IsFalse(Directory.Exists(_dirPath));
    }

    [Test]
    public void Dispose_DeletesDirectoryWithContent()
    {
        TemporaryDirectory.ExecuteIn(_dirPath, dir => {
            File.WriteAllText(dir.GetUniquePath(), "a");
        });

        Assert.IsFalse(Directory.Exists(_dirPath));
    }

    [Test]
    public void Dispose_KeepDirectoryOnException()
    {
        System.Environment.SetEnvironmentVariable(EnvironmentVariables.KeepFilesOnErrorEnvironmentVariable, "yes");

        try
        {
            TemporaryDirectory.ExecuteIn(_dirPath, dir => {
                throw new System.Exception();
            });
        }
        catch(System.Exception)
        {}

        Assert.IsTrue(Directory.Exists(_dirPath));

        Directory.Delete(_dirPath, true);
        System.Environment.SetEnvironmentVariable(EnvironmentVariables.KeepFilesOnErrorEnvironmentVariable, null);
    }

    [Test]
    public void GetUniquePath_ReturnsUniquePaths()
    {
        TemporaryDirectory.ExecuteIn(_dirPath, temporaryData => {
            for (int i = 0; i < 100; i++)
            {
                string path = temporaryData.GetUniquePath();

                Assert.IsFalse(File.Exists(path));
                Assert.IsFalse(Directory.Exists(path));

                if (i%2 == 0)
                {
                    File.WriteAllText(temporaryData.GetUniquePath(), "a");
                }
                else
                {
                    Directory.CreateDirectory(path);
                }
            }
        });
    }
}
#endif