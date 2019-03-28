#if UNITY_2018
using System;
using System.IO;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;

class UnarchiverTest
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

    private void CheckFileConsistency(string sourceFilePath, string filePath)
    {
        var sourceFileInfo = new FileInfo(sourceFilePath);
        var fileInfo = new FileInfo(filePath);

        Assert.That(sourceFileInfo.Length, Is.EqualTo(fileInfo.Length));
    }

    private void CheckConsistency(string sourceDirPath, string dirPath)
    {
        var sourceDirInfo = new DirectoryInfo(sourceDirPath);

        foreach (var file in sourceDirInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
        {
            Assert.That(File.Exists(Path.Combine(dirPath, file.Name)));
            CheckFileConsistency(Path.Combine(sourceDirPath, file.Name), Path.Combine(dirPath, file.Name));
        }

        foreach (var dir in sourceDirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly))
        {
            Assert.That(Directory.Exists(Path.Combine(dirPath, dir.Name)));
            CheckConsistency(Path.Combine(sourceDirPath, dir.Name), Path.Combine(dirPath, dir.Name));
        }
    }

    [Test]
    public void Unarchive()
    {
        var unarchiver = new ZipUnarchiver(TestFixtures.GetFilePath("unarchiver-test/zip.zip"), _dirPath);

        unarchiver.Unarchive(CancellationToken.Empty);

        CheckConsistency(TestFixtures.GetDirectoryPath("unarchiver-test/zip"), _dirPath);
    }

    [Test]
    public void CancelUnarchive()
    {
        var unarchiver = new ZipUnarchiver(TestFixtures.GetFilePath("unarchiver-test/zip.zip"), _dirPath);

        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(() => unarchiver.Unarchive(source), Throws.Exception.TypeOf<OperationCanceledException>());
    }

    [Test]
    public void UnarchiveCorruptedArchive()
    {
        var unarchiver = new ZipUnarchiver(TestFixtures.GetFilePath("unarchiver-test/corrupted-zip.zip"), _dirPath);

        Assert.That(() => unarchiver.Unarchive(CancellationToken.Empty), Throws.Exception);
    }

    [Test]
    public void UnarchiveWithPassword()
    {
        string password = "\x08\x07\x18\x24" + "123==";

        var unarchiver = new ZipUnarchiver(TestFixtures.GetFilePath("unarchiver-test/password-zip.zip"), _dirPath, password);

        unarchiver.Unarchive(CancellationToken.Empty);

        CheckConsistency(TestFixtures.GetDirectoryPath("unarchiver-test/password-zip"), _dirPath);
    }

    [Test]
    public void ProgressReporting()
    {
        var unarchiver = new ZipUnarchiver(TestFixtures.GetFilePath("unarchiver-test/zip.zip"), _dirPath);

        int? lastAmount = null;
        int? lastEntry = null;

        unarchiver.UnarchiveProgressChanged += (name, isFile, entry, amount, entryProgress) =>
        {
            if (!lastAmount.HasValue)
            {
                lastAmount = amount;
            }
            else
            {
                Assert.That(amount, Is.EqualTo(lastAmount.Value));
            }

            if (lastEntry.HasValue)
            {
                Assert.That(entry, Is.GreaterThanOrEqualTo(lastEntry.Value));
            }

            lastEntry = entry;

            Assert.That(entry, Is.GreaterThan(0));

            if (entryProgress == 1.0)
            {
                if (isFile)
                {
                    string filePath = Path.Combine(_dirPath, name);
                    Assert.That(File.Exists(filePath));
                }
                else
                {
                    string dirPath = Path.Combine(_dirPath, name);
                    Assert.That(Directory.Exists(dirPath));
                }
            }

            Assert.That(entryProgress, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(entryProgress, Is.LessThanOrEqualTo(1.0));
        };

        unarchiver.Unarchive(CancellationToken.Empty);

        Assert.That(lastAmount, Is.Not.Null);
        Assert.That(lastEntry, Is.Not.Null);
        Assert.That(lastEntry.Value, Is.EqualTo(lastAmount.Value));
    }
}
#endif