using PatchKit.Api.Models.Keys;
using System.Collections.Generic;

namespace PatchKit.Api
{
    public partial class KeysApiConnection
    {
        
        /// <param name="guid"></param>
        public Job GetJobInfo(string guid)
        {
            string path = "/v2/jobs/{guid}";
            path = path.Replace("{guid}", guid.ToString());
            string query = string.Empty;
            var response = GetResponse(path, query);
            return ParseResponse<Job>(response);
        }
        
        
        
        
        /// <summary>
        /// Gets key info. Required providing an app secret. Will find only key that matches given app_secret. This request registers itself as key usage until valid key_secret is providen with this request.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="appSecret"></param>
        /// <param name="keySecret">If provided and valid, will only do a blocked check.</param>
        public LicenseKey GetKeyInfo(string key, string appSecret, string keySecret = null)
        {
            string path = "/v2/keys/{key}";
            List<string> queryList = new List<string>();
            path = path.Replace("{key}", key.ToString());
            queryList.Add("app_secret="+appSecret);
            if (keySecret != null)
            {
                queryList.Add("key_secret="+keySecret);
            }
            string query = string.Join("&", queryList.ToArray());
            var response = GetResponse(path, query);
            return ParseResponse<LicenseKey>(response);
        }
        
        
        
        
    }
}
