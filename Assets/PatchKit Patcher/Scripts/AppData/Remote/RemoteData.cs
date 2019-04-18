using System;
using System.Linq;
using PatchKit.Api;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class RemoteData : IRemoteData
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(RemoteData));

        private readonly string _appSecret;
        private readonly MainApiConnection _mainApiConnection;

        public RemoteData(string appSecret, IRequestTimeoutCalculator requestTimeoutCalculator)
        {
            Checks.ArgumentNotNullOrEmpty(appSecret, "appSecret");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(appSecret, "appSecret");

            _appSecret = appSecret;

            var mainSettings = Settings.GetMainApiConnectionSettings();

            string overrideMainUrl;

            if (EnvironmentInfo.TryReadEnvironmentVariable(EnvironmentVariables.ApiUrlEnvironmentVariable, out overrideMainUrl))
            {
                var overrideMainUri = new Uri(overrideMainUrl);

                mainSettings.MainServer.Host = overrideMainUri.Host;
                mainSettings.MainServer.Port = overrideMainUri.Port;
                mainSettings.MainServer.UseHttps = overrideMainUri.Scheme == Uri.UriSchemeHttps;
            }

            _mainApiConnection = new MainApiConnection(mainSettings)
            {
                HttpClient = new UnityHttpClient(),
                RequestTimeoutCalculator = requestTimeoutCalculator,
                RequestRetryStrategy = new SimpleInfiniteRequestRetryStrategy(),
                Logger = PatcherLogManager.DefaultLogger
            };
        }

        public RemoteResource GetContentPackageResource(int versionId, string keySecret, string countryCode, CancellationToken cancellationToken)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");

            DebugLogger.Log("Getting content package resource.");
            DebugLogger.LogVariable(versionId, "versionId");
            DebugLogger.LogVariable(keySecret, "keySecret");

            RemoteResource resource = new RemoteResource();

            var summary = _mainApiConnection.GetAppVersionContentSummary(_appSecret, versionId, cancellationToken);
            var urls = _mainApiConnection.GetAppVersionContentUrls(_appSecret, versionId, countryCode, keySecret, cancellationToken);

            resource.Size = summary.Size;
            resource.HashCode = summary.HashCode;
            resource.ChunksData = ConvertToChunksData(summary.Chunks);
            resource.ResourceUrls = urls;

            return resource;
        }

        public RemoteResource GetDiffPackageResource(int versionId, string keySecret, string countryCode, CancellationToken cancellationToken)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");

            DebugLogger.Log("Getting diff package resource.");
            DebugLogger.LogVariable(versionId, "versionId");
            DebugLogger.LogVariable(keySecret, "keySecret");

            RemoteResource resource = new RemoteResource();

            var summary = _mainApiConnection.GetAppVersionDiffSummary(_appSecret, versionId, cancellationToken);
            var urls = _mainApiConnection.GetAppVersionDiffUrls(_appSecret, versionId, countryCode, keySecret, cancellationToken);

            resource.Size = summary.Size;
            resource.HashCode = summary.HashCode;
            resource.ChunksData = ConvertToChunksData(summary.Chunks);
            resource.ResourceUrls = urls;
            return resource;
        }

        public string GetContentPackageResourcePassword(int versionId)
        {
            return new RemoteResourcePasswordGenerator().Generate(_appSecret, versionId);
        }

        public string GetDiffPackageResourcePassword(int versionId)
        {
            return new RemoteResourcePasswordGenerator().Generate(_appSecret, versionId);
        }

        private static ChunksData ConvertToChunksData(Api.Models.Main.Chunks chunks)
        {
            if (chunks.Size == 0 || chunks.Hashes == null)
            {
                return new ChunksData
                {
                    ChunkSize = 0,
                    Chunks = new Chunk[] {}
                };
            }

            var chunksData = new ChunksData
            {
                ChunkSize = chunks.Size,
                Chunks = new Chunk[chunks.Hashes.Length]
            };

            for (int index = 0; index < chunks.Hashes.Length; index++)
            {
                string hash = chunks.Hashes[index];
                var array = XXHashToByteArray(hash);

                chunksData.Chunks[index] = new Chunk
                {
                    Hash = array
                };
            }
            return chunksData;
        }

        // ReSharper disable once InconsistentNaming
        private static byte[] XXHashToByteArray(string hash)
        {
            while (hash.Length < 8)
            {
                hash = "0" + hash;
            }

            byte[] array = Enumerable.Range(0, hash.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hash.Substring(x, 2), 16))
                .ToArray();
            return array;
        }
    }
}