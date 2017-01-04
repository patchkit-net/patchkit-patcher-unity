using System;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;
using PatchKit.Api;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Licensing;
using PatchKit.Unity.Patcher.Net;
using PatchKit.Unity.Patcher.Statistics;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Data
{
    internal class RemoteAppData
    {
        private readonly HttpDownloader _httpDownloader;

        private readonly TorrentDownloader _torrentDownloader;

        private readonly MainApiConnection _mainApiConnection;

        private readonly ILicenseObtainer _licenseObtainer;

        private readonly ILicenseValidator _licenseValidator;

        private readonly string _appSecret;

        private string _keySecret;

        public RemoteAppData(string appSecret, ILicenseObtainer licenseObtainer, ILicenseValidator licenseValidator)
        {
            _appSecret = appSecret;
            _httpDownloader = new HttpDownloader();
            _torrentDownloader = new TorrentDownloader(Application.streamingAssetsPath, 10000);
            _mainApiConnection = new MainApiConnection(Settings.GetMainApiConnectionSettings());
            _licenseObtainer = licenseObtainer;
            _licenseValidator = licenseValidator;
        }

        private string GetKeySecret()
        {
            var app = _mainApiConnection.GetApplicationInfo(_appSecret);

            if (app.UseKeys)
            {
                bool showError = false;

                while (_keySecret == null)
                {
                    _licenseObtainer.ShowError = showError;
                    var license = _licenseObtainer.Obtain();
                    _keySecret = _licenseValidator.Validate(license);
                    showError = true;
                }
            }

            return _keySecret;
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

        /// <summary>
        /// Gets the latest version id from the API server.
        /// </summary>
        public int GetLatestVersionId()
        {
            Debug.Log("Getting latest version id.");

            string path = string.Format("1/apps/{0}/versions/latest/id", _appSecret);

            return _mainApiConnection.GetResponse(path, null).GetJson().Value<int>("id");
        }

        /// <summary>
        /// Gets the content summary from the API server.
        /// </summary>
        public JObject GetContentSummary(int versionId)
        {
            Debug.Log(string.Format("Getting content summary of version with id - {0}", versionId));
            string path = string.Format("1/apps/{0}/versions/{1}/content_summary", _appSecret, versionId);

            return (JObject)_mainApiConnection.GetResponse(path, null).GetJson();
        }

        /// <summary>
        /// Gets the diff summary from the API server.
        /// </summary>
        public JObject GetDiffSummary(int versionId)
        {
            Debug.Log(string.Format("Getting diff summary of version with id - {0}", versionId));
            string path = string.Format("1/apps/{0}/versions/{1}/diff_summary", _appSecret, versionId);

            return (JObject)_mainApiConnection.GetResponse(path, null).GetJson();
        }

        /// <summary>
        /// Downloads the content package.
        /// </summary>
        public void DownloadContentPackage(string contentPackagePath, int versionId, CustomProgressReporter<DownloadProgress> progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log(string.Format("Dowloading content package to {0} for version {1}", contentPackagePath, versionId));

            string keySecret = GetKeySecret();

            try
            {
                var contentTorrentUrl = _mainApiConnection.GetAppVersionContentTorrentUrl(_appSecret, versionId, keySecret);

                DownloadTorrent(contentPackagePath, contentTorrentUrl.Url, progressReporter, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogWarning("Failed to download content package with torrent. Trying to download it through HTTP.");

                var contentUrls = _mainApiConnection.GetAppVersionContentUrls(_appSecret, versionId);

                var contentSummary = _mainApiConnection.GetAppVersionContentSummary(_appSecret, versionId);

                DownloadFileFromUrls(contentPackagePath, contentUrls.Select(url => url.Url).ToArray(), contentSummary.Size, contentSummary.Chunks.Size, contentSummary.Chunks.Hashes, progressReporter, cancellationToken);
            }
        }

        /// <summary>
        /// Downloads the diff package.
        /// </summary>
        public void DownloadDiffPackage(string diffPackagePath, int versionId, CustomProgressReporter<DownloadProgress> progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log(string.Format("Dowloading diff package to {0} for version {1}", diffPackagePath, versionId));

            string keySecret = GetKeySecret();

            try
            {
                var diffTorrentUrl = _mainApiConnection.GetAppVersionDiffTorrentUrl(_appSecret, versionId, keySecret);

                DownloadTorrent(diffPackagePath, diffTorrentUrl.Url, progressReporter, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogWarning("Failed to download diff package with torrent. Trying to download it through HTTP.");

                var diffUrls = _mainApiConnection.GetAppVersionDiffUrls(_appSecret, versionId);

                var diffSummary = _mainApiConnection.GetAppVersionDiffSummary(_appSecret, versionId);

                DownloadFileFromUrls(diffPackagePath, diffUrls.Select(url => url.Url).ToArray(), diffSummary.Size, diffSummary.Chunks.Size, diffSummary.Chunks.Hashes, progressReporter, cancellationToken);
            }
        }
    }
}
