using PatchKit.Api.Models.Main;
using System.Collections.Generic;
using System.Threading;
using CancellationToken = PatchKit.Unity.Patcher.Cancellation.CancellationToken;

namespace PatchKit.Api
{
    public partial class MainApiConnection
    {
        /// <param name="apiKey">Application owner API key.</param>
        public App[] ListsUserApplications(string apiKey, CancellationToken cancellationToken)
        {
            string path = "/1/apps";
            List<string> queryList = new List<string>();
            queryList.Add("api_key="+apiKey);
            string query = string.Join("&", queryList.ToArray());
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<App[]>(response);
        }
        
        /// <summary>
        /// Gets detailes app info
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        public App GetApplicationInfo(string appSecret, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}";
            path = path.Replace("{app_secret}", appSecret.ToString());
            string query = string.Empty;
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<App>(response);
        }
        
        /// <summary>
        /// Gets a complete changelog of an application.
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        public Changelog[] GetAppChangelog(string appSecret, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}/changelog";
            path = path.Replace("{app_secret}", appSecret.ToString());
            string query = string.Empty;
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<Changelog[]>(response);
        }
        
        /// <summary>
        /// Gets the basic information for all published versions. When API Key is provided, draft version information is included if draft version exists.
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        /// <param name="apiKey">Application owner API key.</param>
        public AppVersion[] GetAppVersionList(string appSecret, string apiKey, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}/versions";
            List<string> queryList = new List<string>();
            path = path.Replace("{app_secret}", appSecret.ToString());
            if (apiKey != null)
            {
                queryList.Add("api_key="+apiKey);
            }
            string query = string.Join("&", queryList.ToArray());
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<AppVersion[]>(response);
        }
        
        /// <summary>
        /// Gets latest application version object.
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        public AppVersion GetAppLatestAppVersion(string appSecret, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}/versions/latest";
            path = path.Replace("{app_secret}", appSecret.ToString());
            string query = string.Empty;
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<AppVersion>(response);
        }
        
        /// <summary>
        /// Gets latest application version id. Please use /apps/{app_secret} instead to get the latest version.
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        [System.Obsolete]
        public AppVersionId GetAppLatestAppVersionId(string appSecret, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}/versions/latest/id";
            path = path.Replace("{app_secret}", appSecret.ToString());
            string query = string.Empty;
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<AppVersionId>(response);
        }
        
        /// <summary>
        /// Gets selected version object. If API key is provided, can get the information about draft version.
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        /// <param name="versionId">Version id.</param>
        /// <param name="apiKey">Application owner API key.</param>
        public AppVersion GetAppVersion(string appSecret, int versionId, string apiKey, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}/versions/{version_id}";
            List<string> queryList = new List<string>();
            path = path.Replace("{app_secret}", appSecret.ToString());
            path = path.Replace("{version_id}", versionId.ToString());
            if (apiKey != null)
            {
                queryList.Add("api_key="+apiKey);
            }
            string query = string.Join("&", queryList.ToArray());
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<AppVersion>(response);
        }
        
        /// <summary>
        /// Gets selected version content summary.
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        /// <param name="versionId">Version id.</param>
        public AppContentSummary GetAppVersionContentSummary(string appSecret, int versionId, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}/versions/{version_id}/content_summary";
            path = path.Replace("{app_secret}", appSecret.ToString());
            path = path.Replace("{version_id}", versionId.ToString());
            string query = string.Empty;
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<AppContentSummary>(response);
        }
        
        /// <summary>
        /// Gets selected version diff summary.
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        /// <param name="versionId">Version id.</param>
        public AppDiffSummary GetAppVersionDiffSummary(string appSecret, int versionId, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}/versions/{version_id}/diff_summary";
            path = path.Replace("{app_secret}", appSecret.ToString());
            path = path.Replace("{version_id}", versionId.ToString());
            string query = string.Empty;
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<AppDiffSummary>(response);
        }
        
        /// <summary>
        /// Gets selected application version content torrent url.
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        /// <param name="versionId">Version id.</param>
        /// <param name="keySecret">Key secret provided by key server. This value is optional and is needed only if application is secured by license keys.</param>
        public AppContentTorrentUrl GetAppVersionContentTorrentUrl(string appSecret, int versionId, string keySecret, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}/versions/{version_id}/content_torrent_url";
            List<string> queryList = new List<string>();
            path = path.Replace("{app_secret}", appSecret.ToString());
            path = path.Replace("{version_id}", versionId.ToString());
            if (keySecret != null)
            {
                queryList.Add("key_secret="+keySecret);
            }
            string query = string.Join("&", queryList.ToArray());
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<AppContentTorrentUrl>(response);
        }
        
        /// <summary>
        /// Gets selected application version diff torrent url.
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        /// <param name="versionId">Version id.</param>
        /// <param name="keySecret">Key secret provided by key server. This value is optional and is needed only if application is secured by license keys.</param>
        public AppDiffTorrentUrl GetAppVersionDiffTorrentUrl(string appSecret, int versionId, string keySecret, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}/versions/{version_id}/diff_torrent_url";
            List<string> queryList = new List<string>();
            path = path.Replace("{app_secret}", appSecret.ToString());
            path = path.Replace("{version_id}", versionId.ToString());
            if (keySecret != null)
            {
                queryList.Add("key_secret="+keySecret);
            }
            string query = string.Join("&", queryList.ToArray());
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<AppDiffTorrentUrl>(response);
        }
        
        /// <summary>
        /// Gets selected application version content urls.
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        /// <param name="versionId">Version id.</param>
        /// <param name="country">Country iso code</param>
        /// <param name="keySecret">Key secret provided by key server. This value is optional and is needed only if application is secured by license keys.</param>
        public ResourceUrl[] GetAppVersionContentUrls(string appSecret, int versionId, string country, string keySecret, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}/versions/{version_id}/content_urls";
            List<string> queryList = new List<string>();
            path = path.Replace("{app_secret}", appSecret.ToString());
            path = path.Replace("{version_id}", versionId.ToString());
            if (country != null)
            {
                queryList.Add("country="+country);
            }
            if (keySecret != null)
            {
                queryList.Add("key_secret="+keySecret);
            }
            string query = string.Join("&", queryList.ToArray());
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<ResourceUrl[]>(response);
        }
        
        /// <summary>
        /// Gets selected application version diff urls.
        /// </summary>
        /// <param name="appSecret">Secret of an application.</param>
        /// <param name="versionId">Version id.</param>
        /// <param name="country">Country iso code</param>
        /// <param name="keySecret">Key secret provided by key server. This value is optional and is needed only if application is secured by license keys.</param>
        public ResourceUrl[] GetAppVersionDiffUrls(string appSecret, int versionId, string country, string keySecret, CancellationToken cancellationToken)
        {
            string path = "/1/apps/{app_secret}/versions/{version_id}/diff_urls";
            List<string> queryList = new List<string>();
            path = path.Replace("{app_secret}", appSecret.ToString());
            path = path.Replace("{version_id}", versionId.ToString());
            if (country != null)
            {
                queryList.Add("country="+country);
            }
            if (keySecret != null)
            {
                queryList.Add("key_secret="+keySecret);
            }
            string query = string.Join("&", queryList.ToArray());
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<ResourceUrl[]>(response);
        }
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <param name="apiKey">Application owner API key. Required when not using a session.</param>
        public Plan GetPlanInfo(string apiKey, CancellationToken cancellationToken)
        {
            string path = "/1/me/plan";
            List<string> queryList = new List<string>();
            queryList.Add("api_key="+apiKey);
            string query = string.Join("&", queryList.ToArray());
            var response = GetResponse(path, query, cancellationToken);
            return ParseResponse<Plan>(response);
        }
        
        
        
        
        
        
    }
}
