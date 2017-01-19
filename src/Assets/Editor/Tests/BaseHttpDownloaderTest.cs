using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using NSubstitute;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.Cancellation;

public class BaseHttpDownloaderTest
{
    private byte[] _data;
    private MemoryStream _dataStream;

    [SetUp]
    public void Setup()
    {
        _data = new byte[1024]; // 1 KB
        new Random().NextBytes(_data);
        _dataStream = new MemoryStream(_data, false);
    }

    private IHttpWebResponseAdapter MockResponse(Stream responseStream, HttpStatusCode statusCode)
    {
        var response = Substitute.For<IHttpWebResponseAdapter>();
        response.GetResponseStream().Returns(responseStream);
        response.StatusCode.Returns(statusCode);

        return response;
    }

    private IHttpWebRequestAdapter MockRequest(IHttpWebResponseAdapter response)
    {
        var request = Substitute.For<IHttpWebRequestAdapter>();
        request.GetResponse().Returns(response);

        return request;
    }

    private void ValidateData(MemoryStream stream)
    {
        byte[] buffer = new byte[1];

        for (int i = 0; i < _data.Length; i++)
        {
            Assert.AreEqual(1, stream.Read(buffer, 0, 1), "Invalid data.");
            Assert.AreEqual(_data[i], buffer[0], "Invalid data.");
        }
    }

    [Test]
    public void DataAvailable()
    {
        var baseHttpDownloader = new BaseHttpDownloader("someurl", 10000, 64, url =>
        {
            var response = MockResponse(_dataStream, HttpStatusCode.OK);
            return MockRequest(response);
        });

        MemoryStream readStream = new MemoryStream(_data.Length);

        baseHttpDownloader.DataAvailable += (data, length) =>
        {
            readStream.Write(data, 0, length);
        };

        baseHttpDownloader.Download(CancellationToken.Empty);

        readStream.Seek(0, SeekOrigin.Begin);

        ValidateData(readStream);
    }

    [Test]
    public void InvalidResponse()
    {
        var baseHttpDownloader = new BaseHttpDownloader("someurl", 10000, 64, url =>
        {
            var response = MockResponse(_dataStream, HttpStatusCode.BadRequest);
            return MockRequest(response);
        });
        
        Assert.Catch<DownloaderException>(() => baseHttpDownloader.Download(CancellationToken.Empty));
    }
}