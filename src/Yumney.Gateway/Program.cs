using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using SmartSolutionsLab.Yumney.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddResponseCompression(options =>
{
	options.EnableForHttps = true;
	options.Providers.Add<BrotliCompressionProvider>();
	options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.SmallestSize);

builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
			?? ["http://localhost:4200"];

		policy.WithOrigins(allowedOrigins)
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials();
	});
});

builder.Services.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
	.AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors();

// DEBUG #340: log every incoming request so the E2E workflow can upload it
// as an artifact. Tells us whether the hung browser fetches actually reach
// the gateway, get stuck inside YARP, or never arrive at all. Path is
// configurable via env so the workflow places it where upload-artifact can
// find it; defaults to /tmp on Linux.
var debugLog = Environment.GetEnvironmentVariable("GATEWAY_REQUEST_LOG")
	?? "/tmp/gateway-requests.log";
Console.Error.WriteLine($"[debug] gateway-request-log: {debugLog}");
app.Use(async (context, next) =>
{
	var ts = DateTime.UtcNow;
	var origin = context.Request.Headers.Origin.ToString();
	var method = context.Request.Method;
	var path = context.Request.Path + context.Request.QueryString;
	var startLine = $"{ts:HH:mm:ss.fff} >> {method} {path} Origin={origin}\n";
	Console.Error.Write(startLine);
	try { await File.AppendAllTextAsync(debugLog, startLine); }
	catch (Exception ex) { Console.Error.WriteLine($"[debug] gateway-log write failed: {ex.Message}"); }

	await next();

	var elapsed = (DateTime.UtcNow - ts).TotalMilliseconds;
	var endLine = $"{DateTime.UtcNow:HH:mm:ss.fff} << {context.Response.StatusCode} {method} {path} ({elapsed:0}ms)\n";
	Console.Error.Write(endLine);
	try { await File.AppendAllTextAsync(debugLog, endLine); }
	catch { }
});

// Manual CORS preflight handling for YARP routes.
//
// `UseCors()` only handles OPTIONS preflight when the matched endpoint has
// CORS metadata. YARP routes don't carry that metadata by default, and our
// experiments wiring `CorsPolicy` into YARP route config (#357 v1, #359 v2)
// crashed the gateway at startup. So we handle preflight ourselves before
// `MapReverseProxy` ever sees the request: respond 204 with the right
// Access-Control-* headers when the origin matches our allow-list.
//
// Curl warmup is unaffected — those requests have no `Origin` header, so
// this middleware is a no-op for them.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
	?? ["http://localhost:4200"];

app.Use(async (context, next) =>
{
	var origin = context.Request.Headers.Origin.ToString();
	if (!string.IsNullOrEmpty(origin) && corsAllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
	{
		context.Response.Headers["Access-Control-Allow-Origin"] = origin;
		context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
		context.Response.Headers.Append("Vary", "Origin");

		if (HttpMethods.IsOptions(context.Request.Method))
		{
			context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
			var requestHeaders = context.Request.Headers["Access-Control-Request-Headers"].ToString();
			context.Response.Headers["Access-Control-Allow-Headers"] = string.IsNullOrEmpty(requestHeaders)
				? "*"
				: requestHeaders;
			context.Response.Headers["Access-Control-Max-Age"] = "600";
			context.Response.StatusCode = StatusCodes.Status204NoContent;
			return;
		}
	}

	await next();
});

app.MapDefaultEndpoints()
	.MapReverseProxy();

app.Run();
