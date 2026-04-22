using System;
using System.Net;
using System.Net.Http;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services;

public static class BrowserHttpClientDefaults
{
	public const string UserAgent =
		"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

	public const string Accept =
		"text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

	public const string AcceptLanguage = "en-US,en;q=0.9,de;q=0.8";
	public const string AcceptEncoding = "gzip, deflate, br";

	public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);
	public static readonly Uri DefaultReferrer = new("https://www.google.com/");

	public static void ConfigureHttpClient(HttpClient client)
	{
		client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
		client.DefaultRequestHeaders.Accept.ParseAdd(Accept);
		client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(AcceptLanguage);
		client.DefaultRequestHeaders.AcceptEncoding.ParseAdd(AcceptEncoding);

		client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
		client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
		client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
		client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
		client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
		client.DefaultRequestHeaders.Referrer = DefaultReferrer;

		client.Timeout = RequestTimeout;
	}

	public static HttpClientHandler CreateHandler() => new()
	{
		AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Brotli | DecompressionMethods.Deflate,
		UseCookies = true,
		AllowAutoRedirect = true,
		MaxAutomaticRedirections = 5,
	};
}
