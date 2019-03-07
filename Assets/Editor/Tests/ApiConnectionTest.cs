using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using NSubstitute;
using NUnit.Framework;
using PatchKit.Network;

namespace PatchKit.Api
{
    [TestFixture]
    public class ApiConnectionTest
    {
        private ApiConnectionSettings _apiConnectionSettings;

        [SetUp]
        public void SetUp()
        {
            _apiConnectionSettings = new ApiConnectionSettings
            {
                MainServer = new ApiConnectionServer()
                {
                    Host = "main_server"
                },
                CacheServers = new[]
                {
                    new ApiConnectionServer()
                    {
                        Host = "cache_server_1"
                    },
                    new ApiConnectionServer()
                    {
                        Host = "cache_server_2"
                    }
                }
            };
        }

        [Test]
        public void TestSingle()
        {
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            AddResponseToClient(apiConnection.HttpClient, "http://main_server/path?query",
                CreateSimpleWebResponse("test"));

            var apiResponse = apiConnection.GetResponse("/path", "query");
            Assert.AreEqual("test", apiResponse.Body);
        }

        [Test]
        public void TestHttps()
        {
            _apiConnectionSettings.MainServer.UseHttps = true;
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            AddResponseToClient(apiConnection.HttpClient, "https://main_server/path?query",
                CreateSimpleWebResponse("test"));

            var apiResponse = apiConnection.GetResponse("/path", "query");
            Assert.AreEqual("test", apiResponse.Body);
        }

        [Test]
        public void TestCustomPort()
        {
            _apiConnectionSettings.MainServer.Port = 81;
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            AddResponseToClient(apiConnection.HttpClient, "http://main_server:81/path?query",
                CreateSimpleWebResponse("test"));

            var apiResponse = apiConnection.GetResponse("/path", "query");
            Assert.AreEqual("test", apiResponse.Body);
        }

        [Test]
        public void TestApiCache500()
        {
            // test how connection will behave on 500 error
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            // main
            AddResponseToClient(apiConnection.HttpClient, "http://main_server/path?query",
                CreateErrorResponse(HttpStatusCode.InternalServerError));

            // cache
            AddResponseToClient(apiConnection.HttpClient, "http://cache_server_1/path?query",
                CreateSimpleWebResponse("test"));

            var apiResponse = apiConnection.GetResponse("/path", "query");
            Assert.AreEqual("test", apiResponse.Body);
        }

        [Test]
        public void TestApiCache502()
        {
            // test how connection will behave on 502 error
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            // main
            AddResponseToClient(apiConnection.HttpClient, "http://main_server/path?query",
                CreateErrorResponse(HttpStatusCode.BadGateway));

            // cache
            AddResponseToClient(apiConnection.HttpClient, "http://cache_server_1/path?query",
                CreateSimpleWebResponse("test"));

            var apiResponse = apiConnection.GetResponse("/path", "query");
            Assert.AreEqual("test", apiResponse.Body);
        }

        [Test]
        // ReSharper disable once InconsistentNaming
        public void Test4XXException()
        {
            // any 4XX error from the main server should throw an ApiResponseException
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            AddResponseToClient(apiConnection.HttpClient, "http://main_server/path?query",
                CreateErrorResponse(HttpStatusCode.NotFound));

            Assert.Throws(
                Is.TypeOf<ApiResponseException>(),
                () => apiConnection.GetResponse("/path", "query")
            );
        }

        [Test]
        // ReSharper disable once InconsistentNaming
        public void Test5XXException()
        {
            // any 5XX error from the main server should throw an ApiConnectionException
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            AddResponseToClient(apiConnection.HttpClient, "http://main_server/path?query",
                CreateErrorResponse(HttpStatusCode.InternalServerError));

            var exception = (ApiConnectionException) Assert.Throws(
                Is.TypeOf<ApiConnectionException>(),
                () => apiConnection.GetResponse("/path", "query")
            );
            Assert.IsTrue(exception.MainServerExceptions.Any());
            Assert.IsTrue(exception.CacheServersExceptions.Any());
        }

        [Test]
        // ReSharper disable once InconsistentNaming
        public void TestCache4XXException()
        {
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            AddThrowToClient(apiConnection.HttpClient, "http://main_server/path?query",
                new WebException("main-server"));
            AddThrowToClient(apiConnection.HttpClient, "http://cache_server_1/path?query",
                new WebException("cache-server-1"));
            AddResponseToClient(apiConnection.HttpClient, "http://cache_server_2/path?query",
                CreateErrorResponse(HttpStatusCode.NotFound));

            var exception = (ApiConnectionException) Assert.Throws(
                Is.TypeOf<ApiConnectionException>(),
                () => apiConnection.GetResponse("/path", "query")
            );
            Assert.IsTrue(exception.MainServerExceptions.All(e => e.Message == "main-server"));
            Assert.IsTrue(exception.CacheServersExceptions.Any(e => e.Message == "cache-server-1"));
        }

        [Test]
        public void TestExceptions()
        {
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            AddThrowToClient(apiConnection.HttpClient, "http://main_server/path?query",
                new WebException("main-server"));
            AddThrowToClient(apiConnection.HttpClient, "http://cache_server_1/path?query",
                new WebException("cache-server-1"));
            AddThrowToClient(apiConnection.HttpClient, "http://cache_server_2/path?query",
                new WebException("cache-server-2"));

            var exception = (ApiConnectionException) Assert.Throws(
                Is.TypeOf<ApiConnectionException>(),
                () => apiConnection.GetResponse("/path", "query")
            );
            Assert.IsTrue(exception.MainServerExceptions.All(e => e.Message == "main-server"));
            Assert.IsTrue(exception.CacheServersExceptions.All(e =>
                e.Message == "cache-server-1" || e.Message == "cache-server-2"));
        }

        [Test]
        public void TestErrorOnCache()
        {
            // Any code different than 200 on API cache should skip it
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            // main
            AddResponseToClient(apiConnection.HttpClient, "http://main_server/path?query",
                CreateErrorResponse(HttpStatusCode.BadGateway));

            // cache1
            AddResponseToClient(apiConnection.HttpClient, "http://cache_server_1/path?query",
                CreateErrorResponse(HttpStatusCode.NotFound));

            // cache2
            AddResponseToClient(apiConnection.HttpClient, "http://cache_server_2/path?query",
                CreateSimpleWebResponse("test"));

            var apiResponse = apiConnection.GetResponse("/path", "query");
            Assert.AreEqual("test", apiResponse.Body);
        }

        [Test]
        public void TestGetContentUrls()
        {
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            AddResponseToClient(apiConnection.HttpClient, "http://main_server/1/apps/secret/versions/13/content_urls",
                CreateSimpleWebResponse(
                    "[{\"url\": \"http://first\", \"meta_url\": \"http://efg\", \"country\": \"PL\"}, " +
                    "{\"url\": \"http://second\", \"meta_url\": \"http://efg\"}]"));

            var contentUrls = apiConnection.GetAppVersionContentUrls("secret", 13);
            Assert.AreEqual(2, contentUrls.Length);
            Assert.AreEqual("http://first", contentUrls[0].Url);
            Assert.AreEqual("PL", contentUrls[0].Country);
            Assert.AreEqual(null, contentUrls[1].Country);
        }

        [Test]
        public void TestGetContentUrlsWithCountry()
        {
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            AddResponseToClient(apiConnection.HttpClient,
                "http://main_server/1/apps/secret/versions/13/content_urls?country=PL",
                CreateSimpleWebResponse(
                    "[{\"url\": \"http://first\", \"meta_url\": \"http://efg\", \"country\": \"PL\"}, " +
                    "{\"url\": \"http://second\", \"meta_url\": \"http://efg\"}]"));

            var contentUrls = apiConnection.GetAppVersionContentUrls("secret", 13, "PL");

            Assert.AreEqual(2, contentUrls.Length);
            Assert.AreEqual("http://first", contentUrls[0].Url);
            Assert.AreEqual("PL", contentUrls[0].Country);
            Assert.AreEqual(null, contentUrls[1].Country);
        }

        [Test]
        public void TestGetDiffUrls()
        {
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            AddResponseToClient(apiConnection.HttpClient, "http://main_server/1/apps/secret/versions/13/diff_urls",
                CreateSimpleWebResponse(
                    "[{\"url\": \"http://first\", \"meta_url\": \"http://efg\", \"country\": \"PL\"}, " +
                    "{\"url\": \"http://second\", \"meta_url\": \"http://efg\"}]"));

            var contentUrls = apiConnection.GetAppVersionDiffUrls("secret", 13);
            Assert.AreEqual(2, contentUrls.Length);
            Assert.AreEqual("http://first", contentUrls[0].Url);
            Assert.AreEqual("PL", contentUrls[0].Country);
            Assert.AreEqual(null, contentUrls[1].Country);
        }

        [Test]
        public void TestGetDiffUrlsWithCountry()
        {
            var apiConnection = new MainApiConnection(_apiConnectionSettings)
            {
                HttpClient = Substitute.For<IHttpClient>()
            };

            AddResponseToClient(apiConnection.HttpClient,
                "http://main_server/1/apps/secret/versions/13/diff_urls?country=PL",
                CreateSimpleWebResponse(
                    "[{\"url\": \"http://first\", \"meta_url\": \"http://efg\", \"country\": \"PL\"}, " +
                    "{\"url\": \"http://second\", \"meta_url\": \"http://efg\"}]"));

            var contentUrls = apiConnection.GetAppVersionDiffUrls("secret", 13, "PL");

            Assert.AreEqual(2, contentUrls.Length);
            Assert.AreEqual("http://first", contentUrls[0].Url);
            Assert.AreEqual("PL", contentUrls[0].Country);
            Assert.AreEqual(null, contentUrls[1].Country);
        }

        private static void AddResponseToClient(IHttpClient client, string url, IHttpResponse response)
        {
            client.Get(Arg.Is<HttpGetRequest>(r => r.Address.ToString() == url)).Returns(response);
        }

        private static void AddThrowToClient(IHttpClient client, string url, Exception exception)
        {
            client.Get(Arg.Is<HttpGetRequest>(r => r.Address.ToString() == url)).Returns(_ => throw exception);
        }

        private static IHttpResponse CreateErrorResponse(HttpStatusCode statusCode)
        {
            var mainResponse = Substitute.For<IHttpResponse>();
            mainResponse.StatusCode.Returns(statusCode);
            return mainResponse;
        }

        private static IHttpResponse CreateSimpleWebResponse(string str)
        {
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(str));

            var webResponse = Substitute.For<IHttpResponse>();
            webResponse.ContentStream.Returns(memoryStream);
            webResponse.CharacterSet.Returns("UTF-8");
            webResponse.StatusCode.Returns(HttpStatusCode.OK);
            return webResponse;
        }
    }
}