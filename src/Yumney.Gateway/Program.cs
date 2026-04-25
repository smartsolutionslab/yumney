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

// Register the same policy under both the default slot (so app.UseCors()
// applies it globally) and a named "default" slot (so YARP routes can
// reference it via "CorsPolicy": "default" — without that, OPTIONS
// preflights are forwarded to upstream APIs and the browser sees
// "Failed to fetch").
builder.Services.AddCors(options =>
{
	var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
		?? ["http://localhost:4200"];

	void Configure(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy) =>
		policy.WithOrigins(allowedOrigins)
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials();

	options.AddDefaultPolicy(Configure);
	options.AddPolicy("default", Configure);
});

builder.Services.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
	.AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseHsts();
	app.UseHttpsRedirection();
}

// CORS must run BEFORE the reverse proxy so OPTIONS preflight short-circuits
// here instead of being forwarded upstream (where most APIs return 405 and
// the browser then blocks the actual request with "Failed to fetch").
app.UseCors();
app.UseResponseCompression();

app.MapDefaultEndpoints()
	.MapReverseProxy();

app.Run();
