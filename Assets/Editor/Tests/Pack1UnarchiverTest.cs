using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;

public class Pack1UnarchiverTest
{
    private const string Key = "test123";
    private string _tempDir;

    [SetUp]
    public void SetUp()
    {
        _tempDir = TestHelpers.CreateTemporaryDirectory();
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.DeleteTemporaryDirectory(_tempDir);
    }

    [Test]
    public void Unpack()
    {
        string archivePath = TestFixtures.GetFilePath("pack1/test.pack1");
        string metaPath = TestFixtures.GetFilePath("pack1/test.pack1.meta");
        string metaString = File.ReadAllText(metaPath);
        Pack1Meta meta = Pack1Meta.Parse(metaString);

        var pack1Unarchiver = new Pack1Unarchiver(archivePath, meta, _tempDir, Key);
        pack1Unarchiver.Unarchive(new CancellationToken());

        Assert.True(Directory.Exists(Path.Combine(_tempDir, "dir")));

        var rakefile = Path.Combine(_tempDir, "dir/Rakefile");
        Assert.True(File.Exists(rakefile));
        Assert.AreEqual("d2974b45f816b3ddaca7a984a9101707", Md5File(rakefile));

        var rubocopFile = Path.Combine(_tempDir, ".rubocop.yml");
        Assert.True(File.Exists(rubocopFile));
        Assert.AreEqual("379cc2261c048e4763969cca74974237", Md5File(rubocopFile));
    }

    private string Md5File(string path)
    {
        using (MD5 md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(path))
            {
                byte[] bytes = md5.ComputeHash(stream);
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}