using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSolutionsLab.Yumney.Shared.Capabilities;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Discovery;

/// <summary>
/// Background service that fetches the capability manifest from each module
/// host on startup and pokes the result into <see cref="AggregatedCapabilityRegistry"/>.
/// Failures are logged and skipped — a transient outage on one module must not
/// stop the MCP server from booting.
/// </summary>
#pragma warning disable SA1303
internal sealed partial class CapabilityDiscoveryService(
	IHttpClientFactory httpClientFactory,
	AggregatedCapabilityRegistry registry,
	ILogger<CapabilityDiscoveryService> logger) : BackgroundService
{
	private const string wellKnownPath = "/.well-known/yumney-capabilities";

	// Retry cadence for hosts that haven't responded yet. Module hosts can
	// reach KnownResourceStates.Running before their HTTP listener has fully
	// warmed up (recipes-api in particular — SK kernel + extraction service
	// registration). The standard resilience handler around the discovery
	// HttpClient already caps individual attempts at 30s; this outer loop
	// retries the still-missing services on a 5s cadence until either every
	// known host has reported or the discovery service is cancelled.
	private const int maxRetryRounds = 24;

	// Module hosts add JsonStringEnumConverter to their response serializer
	// (HostBuilderExtensions.ConfigureHttpJsonOptions). CapabilitySurface is a
	// [Flags] enum that ships on the wire as a comma-joined string like
	// "Chat, Mcp" — default GetFromJsonAsync options can't read it back into
	// the enum and the discovery fetch fails with a JsonException pointing at
	// $.capabilities[0].surfaces.
#pragma warning disable SA1311
	private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};

	private static readonly TimeSpan retryInterval = TimeSpan.FromSeconds(5);
#pragma warning restore SA1311

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		for (var round = 0; round < maxRetryRounds; round++)
		{
			if (stoppingToken.IsCancellationRequested) return;

			foreach (var serviceName in KnownCapabilityHosts.ServiceNames)
			{
				if (stoppingToken.IsCancellationRequested) return;
				if (registry.Manifests.ContainsKey(serviceName)) continue;
				await DiscoverFromServiceAsync(serviceName, stoppingToken);
			}

			if (registry.Manifests.Count == KnownCapabilityHosts.ServiceNames.Count) break;

			try
			{
				await Task.Delay(retryInterval, stoppingToken);
			}
			catch (OperationCanceledException)
			{
				return;
			}
		}

		LogDiscoveryComplete(registry.Manifests.Count, registry.CapabilityCount);
	}

	private async Task DiscoverFromServiceAsync(string serviceName, CancellationToken cancellationToken)
	{
		try
		{
			var client = httpClientFactory.CreateClient(serviceName);
			var manifest = await client.GetFromJsonAsync<CapabilityManifest>(wellKnownPath, jsonOptions, cancellationToken);
			if (manifest is null)
			{
				LogManifestNullFor(serviceName);
				registry.RemoveManifest(serviceName);
				return;
			}

			registry.SetManifest(serviceName, manifest);
			LogManifestFor(serviceName, manifest.Capabilities.Count);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			LogManifestFetchFailed(serviceName, ex.Message);
			registry.RemoveManifest(serviceName);
		}
	}

	[LoggerMessage(Level = LogLevel.Information, Message = "Discovery complete: {ServiceCount} services, {CapabilityCount} total capabilities.")]
	private partial void LogDiscoveryComplete(int serviceCount, int capabilityCount);

	[LoggerMessage(Level = LogLevel.Information, Message = "Discovered {CapabilityCount} capabilities from {ServiceName}.")]
	private partial void LogManifestFor(string serviceName, int capabilityCount);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Manifest endpoint at {ServiceName} returned null payload.")]
	private partial void LogManifestNullFor(string serviceName);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Failed to fetch capability manifest from {ServiceName}: {Reason}")]
	private partial void LogManifestFetchFailed(string serviceName, string reason);
}
