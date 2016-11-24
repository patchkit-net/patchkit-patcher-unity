using System;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using PatchKit.Api;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Net;
using PatchKit.Unity.Patcher.Statistics;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Data
{
    internal class RemoteAppData
    {
        private readonly HttpDownloader _httpDownloader;

        private readonly TorrentDownloader _torrentDownloader;

        private readonly ApiConnection _apiConnection;

        private readonly string _appSecret;

        public RemoteAppData(string appSecret)
        {
            _appSecret = appSecret;
            _httpDownloader = new HttpDownloader();
            _torrentDownloader = new TorrentDownloader(Application.streamingAssetsPath);
            _apiConnection = new ApiConnection(Settings.GetApiConnectionSettings());
        }

        private void DownloadFileFromUrls(string destinationFilePath, string[] sourceFileUrls, long totalBytes, int chunkSize, string[] chunkHashes,
            CustomProgressReporter<DownloadProgress> progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log("Downloading file from urls" + string.Join("; ", sourceFileUrls));
            bool downloaded = false;
            if (chunkSize > 0)
            {
                using (var chunkedDownloader = new ChunkedFileDownloader(sourceFileUrls, totalBytes, destinationFilePath,
                        chunkSize, chunkHashes))
                {
                    downloaded = chunkedDownloader.Start(progressReporter, cancellationToken);
                }
            }
            else
            {
                foreach (string sourceFileUrl in sourceFileUrls)
                {
                    try
                    {
                        _httpDownloader.DownloadFile(sourceFileUrl, destinationFilePath, totalBytes, progressReporter,
                            cancellationToken);

                        downloaded = true;
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                        Debug.LogWarning(string.Format("Failed to download file from {0}", sourceFileUrl));
                    }
                }
            }
            if (!downloaded)
            {
                throw new WebException("Failed to download file from urls: " + string.Join("; ", sourceFileUrls));
            }
        }

        private void DownloadTorrent(string destinationFilePath, string torrentFileUrl, CustomProgressReporter<DownloadProgress> progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log(string.Format("Downloading file from torrent {0}", torrentFileUrl));

            string torrentFilePath = destinationFilePath + ".torrent";

            try
            {
                _httpDownloader.DownloadFile(torrentFileUrl, torrentFilePath, 0, new CustomProgressReporter<DownloadProgress>(), cancellationToken);

                _torrentDownloader.DownloadFile(torrentFilePath, destinationFilePath, progressReporter,
                    cancellationToken);
            }
            finally
            {
                if (File.Exists(torrentFilePath))
                {
                    File.Delete(torrentFilePath);
                }
            }
        }

        private string GetContentTorrentUrl(int versionId)
        {
            Debug.Log(string.Format("Getting content torrent url of version with id - {0}", versionId));
            string path = string.Format("1/apps/{0}/versions/{1}/content_torrent_url", _appSecret, versionId);

            return ((JObject)_apiConnection.GetResponse(path, null).GetJson()).Value<string>("url");
        }

        private string GetDiffTorrentUrl(int versionId)
        {
            Debug.Log(string.Format("Getting diff torrent url of version with id - {0}", versionId));
            string path = string.Format("1/apps/{0}/versions/{1}/diff_torrent_url", _appSecret, versionId);

            return ((JObject)_apiConnection.GetResponse(path, null).GetJson()).Value<string>("url");
        }

        private string[] GetContentUrls(int versionId)
        {
            Debug.Log(string.Format("Getting content urls of version with id - {0}", versionId));
            string path = string.Format("1/apps/{0}/versions/{1}/content_urls", _appSecret, versionId);

            return ((JArray)_apiConnection.GetResponse(path, null).GetJson()).Select(token => token.Value<string>("url")).ToArray();
        }

        private string[] GetDiffUrls(int versionId)
        {
            Debug.Log(string.Format("Getting content urls of version with id - {0}", versionId));
            string path = string.Format("1/apps/{0}/versions/{1}/diff_urls", _appSecret, versionId);

            return ((JArray)_apiConnection.GetResponse(path, null).GetJson()).Select(token => token.Value<string>("url")).ToArray();
        }

        /// <summary>
        /// Gets the latest version id from the API server.
        /// </summary>
        public int GetLatestVersionId()
        {
            Debug.Log("Getting latest version id.");

            string path = string.Format("1/apps/{0}/versions/latest/id", _appSecret);

            return _apiConnection.GetResponse(path, null).GetJson().Value<int>("id");
        }

        /// <summary>
        /// Gets the content summary from the API server.
        /// </summary>
        public JObject GetContentSummary(int versionId)
        {
            Debug.Log(string.Format("Getting content summary of version with id - {0}", versionId));
            string path = string.Format("1/apps/{0}/versions/{1}/content_summary", _appSecret, versionId);

            return (JObject)_apiConnection.GetResponse(path, null).GetJson();
        }

        /// <summary>
        /// Gets the diff summary from the API server.
        /// </summary>
        public JObject GetDiffSummary(int versionId)
        {
            Debug.Log(string.Format("Getting diff summary of version with id - {0}", versionId));
            string path = string.Format("1/apps/{0}/versions/{1}/diff_summary", _appSecret, versionId);

            return (JObject)_apiConnection.GetResponse(path, null).GetJson();
        }

        /// <summary>
        /// Downloads the content package.
        /// </summary>
        public void DownloadContentPackage(string contentPackagePath, int versionId, CustomProgressReporter<DownloadProgress> progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log(string.Format("Dowloading content package to {0} for version {1}", contentPackagePath, versionId));

            try
            {
                var contentTorrentUrl = GetContentTorrentUrl(versionId);

                DownloadTorrent(contentPackagePath, contentTorrentUrl, progressReporter, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogWarning("Failed to download content package with torrent. Trying to download it through HTTP.");

                var contentUrls = GetContentUrls(versionId);

                var contentSummary = _apiConnection.GetAppVersionContentSummary(_appSecret, versionId);

                DownloadFileFromUrls(contentPackagePath, contentUrls, contentSummary.Size, contentSummary.Chunks.Size, contentSummary.Chunks.Hashes, progressReporter, cancellationToken);
            }
        }

        /// <summary>
        /// Downloads the diff package.
        /// </summary>
        public void DownloadDiffPackage(string diffPackagePath, int versionId, CustomProgressReporter<DownloadProgress> progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log(string.Format("Dowloading diff package to {0} for version {1}", diffPackagePath, versionId));

            try
            {
                var diffTorrentUrl = GetDiffTorrentUrl(versionId);

                DownloadTorrent(diffPackagePath, diffTorrentUrl, progressReporter, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogWarning("Failed to download diff package with torrent. Trying to download it through HTTP.");

                var diffUrls = GetDiffUrls(versionId);

                var diffSummary = _apiConnection.GetAppVersionDiffSummary(_appSecret, versionId);

                DownloadFileFromUrls(diffPackagePath, diffUrls, diffSummary.Size, diffSummary.Chunks.Size, diffSummary.Chunks.Hashes, progressReporter, cancellationToken);
            }
        }
    }
}
