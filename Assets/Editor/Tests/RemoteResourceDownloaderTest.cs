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
        return new ChunksData()
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
            TorrentUrls = new[] {"torrent-url"},
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
    public void UseTorrentDownloaderFirst()
    {
        RemoteResource resource = CreateTestRemoteResource();

        var httpDownloader = Substitute.For<IHttpDownloader>();
        var chunkedHttpDownloader = Substitute.For<IChunkedHttpDownloader>();
        var torrentDownloader = Substitute.For<ITorrentDownloader>();

        var downloader = new RemoteResourceDownloader(_filePath, _metaFilePath, resource, true,
            (path, remoteResource, timeout) => httpDownloader,
            (path, remoteResource, timeout) => chunkedHttpDownloader,
            (path, remoteResource, timeout) => torrentDownloader);

        downloader.Download(CancellationToken.Empty);

        httpDownloader.DidNotReceiveWithAnyArgs().Download(CancellationToken.Empty);
        chunkedHttpDownloader.DidNotReceiveWithAnyArgs().Download(CancellationToken.Empty);
        torrentDownloader.ReceivedWithAnyArgs().Download(CancellationToken.Empty);
    }

    [Test]
    public void UseChunkedHttpDownloaderIfTorrentFails()
    {
        RemoteResource resource = CreateTestRemoteResource();

        var httpDownloader = Substitute.For<IHttpDownloader>();
        var chunkedHttpDownloader = Substitute.For<IChunkedHttpDownloader>();
        var torrentDownloader = Substitute.For<ITorrentDownloader>();
        torrentDownloader.When(t => t.Download(CancellationToken.Empty)).Do(
            info => { throw new DownloaderException("Test.", DownloaderExceptionStatus.Other); });

        var downloader = new RemoteResourceDownloader(_filePath, _metaFilePath, resource, true,
            (path, remoteResource, timeout) => httpDownloader,
            (path, remoteResource, timeout) => chunkedHttpDownloader,
            (path, remoteResource, timeout) => torrentDownloader);

        downloader.Download(CancellationToken.Empty);

        httpDownloader.DidNotReceiveWithAnyArgs().Download(CancellationToken.Empty);
        chunkedHttpDownloader.ReceivedWithAnyArgs().Download(CancellationToken.Empty);
        torrentDownloader.ReceivedWithAnyArgs().Download(CancellationToken.Empty);
    }

    [Test]
    public void UseChunkedHttpDownloaderIfTorrentIsNotUsed()
    {
        RemoteResource resource = CreateTestRemoteResource();

        var httpDownloader = Substitute.For<IHttpDownloader>();
        var chunkedHttpDownloader = Substitute.For<IChunkedHttpDownloader>();
        var torrentDownloader = Substitute.For<ITorrentDownloader>();

        var downloader = new RemoteResourceDownloader(_filePath, _metaFilePath, resource, false,
            (path, remoteResource, timeout) => httpDownloader,
            (path, remoteResource, timeout) => chunkedHttpDownloader,
            (path, remoteResource, timeout) => torrentDownloader);

        downloader.Download(CancellationToken.Empty);

        httpDownloader.DidNotReceiveWithAnyArgs().Download(CancellationToken.Empty);
        chunkedHttpDownloader.ReceivedWithAnyArgs().Download(CancellationToken.Empty);
        torrentDownloader.DidNotReceiveWithAnyArgs().Download(CancellationToken.Empty);
    }

    [Test]
    public void UseHttpDownloaderIfChunksAreNotAvailable()
    {
        RemoteResource resource = CreateTestRemoteResource();
        resource.ChunksData.Chunks = new Chunk[0];

        var httpDownloader = Substitute.For<IHttpDownloader>();
        var chunkedHttpDownloader = Substitute.For<IChunkedHttpDownloader>();
        var torrentDownloader = Substitute.For<ITorrentDownloader>();

        var downloader = new RemoteResourceDownloader(_filePath, _metaFilePath, resource, false,
            (path, remoteResource, timeout) => httpDownloader,
            (path, remoteResource, timeout) => chunkedHttpDownloader,
            (path, remoteResource, timeout) => torrentDownloader);

        downloader.Download(CancellationToken.Empty);

        httpDownloader.ReceivedWithAnyArgs().Download(CancellationToken.Empty);
        chunkedHttpDownloader.DidNotReceiveWithAnyArgs().Download(CancellationToken.Empty);
        torrentDownloader.DidNotReceiveWithAnyArgs().Download(CancellationToken.Empty);
    }
}