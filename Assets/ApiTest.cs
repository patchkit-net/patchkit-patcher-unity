using System;
using UnityEngine;
using System.Net;
using Newtonsoft.Json.Linq;
using PatchKit.Api;

public class ApiTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
		ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
		{
			Debug.Log("aaa");
			return true;
		};		
		
		var uri = new UriBuilder()
		{
			Scheme = "https",
			Host = "ip2loc.patchkit.net",
			Path = "v1/country"
		}.Uri;

		var webRequest = WebRequest.Create(uri);
		var webResponse = webRequest.GetResponse();
		Debug.Log(webResponse.ContentLength);

		var apiConnectionSettings = new ApiConnectionSettings
		{
			CacheServers = new string[0],
			MainServer = "ip2loc.patchkit.net",
			Timeout = 10000,
			UseHttps = true
		};
                
		var apiConnection = new ApiConnection(apiConnectionSettings);
		
		var countryResponse = apiConnection.GetResponse("/v1/country", null);
		JToken jToken = countryResponse.GetJson();
		Debug.Log(jToken.ToString());
	}
}
