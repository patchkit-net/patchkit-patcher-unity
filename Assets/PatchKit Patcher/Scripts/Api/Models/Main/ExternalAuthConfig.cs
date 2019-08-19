using Newtonsoft.Json;

namespace PatchKit.Api.Models.Main
{
    public struct ExternalAuthConfig
    {
        [JsonProperty("id")]
        public string Id;
        
        [JsonProperty("include_refresh_token")]
        public string IncludeRefreshToken;
        
        [JsonProperty("refresh_token_endpoint_url")]
        public string RefreshTokenEndpointUrl;
        
        [JsonProperty("refresh_token_endpoint_unauthorized_response")]
        public int RefreshTokenEndpointUnauthorizedResponse;
        
        [JsonProperty("refresh_token_endpoint_ok_response")]
        public int RefreshTokenEndpointOkResponse;
        
        [JsonProperty("refresh_token_path")]
        public string RefreshTokenPath;
        
        [JsonProperty("login_page_url")]
        public string LoginPageUrl;
        
        [JsonProperty("request_id_param")]
        public string RequestIdParam;
        
        [JsonProperty("endpoint_url")]
        public string EndpointUrl;
        
        [JsonProperty("endpoint_not_recognized_response")]
        public int EndpointNotRecognizedResponse;
        
        [JsonProperty("endpoint_wait_response")]
        public int EndpointWaitResponse;
        
        [JsonProperty("endpoint_forbidden_response")]
        public int EndpointForbiddenResponse;
        
        [JsonProperty("endpoint_ok_response")]
        public int EndpointOkResponse;
        
        [JsonProperty("endpoint_query_interval_seconds")]
        public int EndpointQueryIntervalSeconds;
        
        [JsonProperty("endpoint_query_timeout_seconds")]
        public int EndpointQueryTimeoutSeconds;
        
        [JsonProperty("execution_args_path")]
        public string ExecutionArgsPath;
        
        [JsonProperty("message_path")]
        public string MessagePath;
        
        [JsonProperty("patcher_wait_behavior")]
        public string PatcherWaitBehavior;
        
    }
}
