#if UNITY_2018
using System;
using System.IO;
using NSubstitute;
using NUnit.Framework;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.Cancellation;

public class RemoteResourceDownloaderTest
{
    private string _dirPath;
    private string _filePath;
    private string _metaFilePath;
    private byte[] _fileData;

    private ChunksData CreateTestChunksData()
    {
        return new ChunksData
        {
            Chunks = new[]
            {
                new Chunk
                {
                    Hash = new byte[] {0}
                }
            },
            ChunkSize = 1
        };
    }

    private RemoteResource CreateTestRemoteResource()
    {
        return new RemoteResource
        {
            ChunksData = CreateTestChunksData(),
            Size = 1,
            HashCode = "hashcode",
            ResourceUrls = new[]
            {
                // TODO: Test when MetaUrl is set
                new ResourceUrl
                {
                    Url = "url-1",
                    MetaUrl = null,
                    Country = "PL",
                    PartSize = 0,
                },
                new ResourceUrl
                {
                    Url = "url-2",
                    MetaUrl = null,
                    Country = "PL",
                    PartSize = 0,
                }
            }
        };
    }

    [SetUp]
    public void Setup()
    {
        _dirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _filePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _metaFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _fileData = new byte[1024];

        new Random().NextBytes(_fileData);

        Directory.CreateDirectory(_dirPath);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
        if (File.Exists(_metaFilePath))
        {
            File.Delete(_metaFilePath);
        }
        if (Directory.Exists(_dirPath))
        {
            Directory.Delete(_dirPath, true);
        }
    }

    [Test]
    public void UseChunkedHttpDownloader()
    {
        RemoteResource resource = CreateTestRemoteResource();

        var httpDownloader = Substitute.For<IHttpDownloader>();
        var chunkedHttpDownloader = Substitute.For<IChunkedHttpDownloader>();

        var downloader = new RemoteResourceDownloader(_filePath, _metaFilePath, resource,
            (path, urls) => httpDownloader,
            (path, urls, data, size) => chunkedHttpDownloader);

        downloader.Download(CancellationToken.Empty);

        httpDownloader.DidNotReceiveWithAnyArgs().Download(CancellationToken.Empty);
        chunkedHttpDownloader.ReceivedWithAnyArgs().Download(CancellationToken.Empty);
    }

    [Test]
    public void UseHttpDownloaderIfChunksAreNotAvailable()
    {
        RemoteResource resource = CreateTestRemoteResource();
        resource.ChunksData.Chunks = new Chunk[0];

        var httpDownloader = Substitute.For<IHttpDownloader>();
        var chunkedHttpDownloader = Substitute.For<IChunkedHttpDownloader>();

        var downloader = new RemoteResourceDownloader(_filePath, _metaFilePath, resource,
            (path, urls) => httpDownloader,
            (path, urls, data, size) => chunkedHttpDownloader);

        downloader.Download(CancellationToken.Empty);

        httpDownloader.ReceivedWithAnyArgs().Download(CancellationToken.Empty);
        chunkedHttpDownloader.DidNotReceiveWithAnyArgs().Download(CancellationToken.Empty);
    }
}
#endif