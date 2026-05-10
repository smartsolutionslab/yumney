using System.Net.Http.Json;
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

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		foreach (var serviceName in KnownCapabilityHosts.ServiceNames)
		{
			if (stoppingToken.IsCancellationRequested) return;
			await DiscoverFromServiceAsync(serviceName, stoppingToken);
		}

		LogDiscoveryComplete(registry.Manifests.Count, registry.CapabilityCount);
	}

	private async Task DiscoverFromServiceAsync(string serviceName, CancellationToken cancellationToken)
	{
		try
		{
			var client = httpClientFactory.CreateClient(serviceName);
			var manifest = await client.GetFromJsonAsync<CapabilityManifest>(wellKnownPath, cancellationToken);
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
