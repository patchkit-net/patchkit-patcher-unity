using System.IO;
using NSubstitute;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppUpdater.Commands;

class InstallContentCommandTest
{
    private string _dirPath;

    [SetUp]
    public void Setup()
    {
        _dirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_dirPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_dirPath))
        {
            Directory.Delete(_dirPath, true);
        }
    }

    [Test]
    public void Install()
    {
        var localData = Substitute.For<ILocalData>();
        localData.WhenForAnyArgs(data => data.CreateDirectory(null)).Do(info => Directory.CreateDirectory(Path.Combine(_dirPath, (string)info[0])));
        localData.WhenForAnyArgs(data => data.CreateOrUpdateFile(null, null)).Do(info =>
        {
            
        });

        //localData.DownloadData.Returns(new DownloadData(Path.Combine(_dirPath, "download")));
        //localData.TemporaryData.Returns(new TemporaryData(Path.Combine(_dirPath, "temp")));

        var remoteData = Substitute.For<IRemoteData>();

        //var command = new InstallContentCommand(1, )
    }
}