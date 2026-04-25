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
	var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
		?? ["http://localhost:4200"];

	void Configure(Microsoft.AspNetCore.Cors.Infrastructure.CorsPolicyBuilder policy) =>
		policy.WithOrigins(allowedOrigins)
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials();

	options.AddDefaultPolicy(Configure);

	// YARP routes reference policies by name via "CorsPolicy" config — without
	// a named registration, browser preflights fall through to the upstream
	// API where most endpoints return 405 and the browser blocks the actual
	// request with "TypeError: Failed to fetch". Mirrors the default policy.
	options.AddPolicy("Default", Configure);
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

app.MapDefaultEndpoints()
	.MapReverseProxy();

app.Run();
