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
