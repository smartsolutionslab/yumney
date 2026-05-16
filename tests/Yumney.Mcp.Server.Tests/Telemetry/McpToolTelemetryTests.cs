using FluentAssertions;
using SmartSolutionsLab.Yumney.Mcp.Server.Telemetry;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.Telemetry;

public class McpToolTelemetryTests
{
	[Theory]
	[InlineData(200)]
	[InlineData(201)]
	[InlineData(204)]
	[InlineData(299)]
	public void OutcomeFromHttpStatus_2xx_IsSuccess(int statusCode)
	{
		McpToolTelemetry.OutcomeFromHttpStatus(statusCode).Should().Be(McpToolTelemetry.Outcomes.Success);
	}

	[Theory]
	[InlineData(400)]
	[InlineData(401)]
	[InlineData(404)]
	[InlineData(429)]
	[InlineData(499)]
	public void OutcomeFromHttpStatus_4xx_IsClientError(int statusCode)
	{
		McpToolTelemetry.OutcomeFromHttpStatus(statusCode).Should().Be(McpToolTelemetry.Outcomes.ClientError);
	}

	[Theory]
	[InlineData(500)]
	[InlineData(502)]
	[InlineData(503)]
	[InlineData(504)]
	[InlineData(599)]
	public void OutcomeFromHttpStatus_5xx_IsServerError(int statusCode)
	{
		McpToolTelemetry.OutcomeFromHttpStatus(statusCode).Should().Be(McpToolTelemetry.Outcomes.ServerError);
	}

	[Theory]
	[InlineData(100)]
	[InlineData(199)]
	[InlineData(300)]
	[InlineData(399)]
	public void OutcomeFromHttpStatus_Non4xx5xxNon2xx_FallsThroughToServerError(int statusCode)
	{
		// Anything outside 2xx/4xx is bucketed as server_error — non-2xx redirects
		// shouldn't reach this code path (HttpClient follows them by default), but
		// if one does, surfacing it as server_error is louder than success.
		McpToolTelemetry.OutcomeFromHttpStatus(statusCode).Should().Be(McpToolTelemetry.Outcomes.ServerError);
	}

	[Fact]
	public void MeterName_MatchesYumneyWildcard()
	{
		// ServiceDefaults registers `AddMeter("Yumney.*")` — our meter MUST live
		// under that namespace or it's silently dropped from the exporter.
		McpToolTelemetry.MeterName.Should().StartWith("Yumney.");
	}

	[Fact]
	public void ActivitySourceName_MatchesYumneyWildcard()
	{
		McpToolTelemetry.ActivitySourceName.Should().StartWith("Yumney.");
	}

	[Fact]
	public void Outcomes_AreLowercaseSnakeCase()
	{
		// Tag values are lowered to keep cardinality math simple in PromQL /
		// App Insights KQL — Outcome enum values like "ClientError" would
		// shadow casing-only dupes.
		string[] all =
		[
			McpToolTelemetry.Outcomes.Success,
			McpToolTelemetry.Outcomes.ClientError,
			McpToolTelemetry.Outcomes.ServerError,
			McpToolTelemetry.Outcomes.Timeout,
			McpToolTelemetry.Outcomes.UnknownTool,
			McpToolTelemetry.Outcomes.MissingArguments,
			McpToolTelemetry.Outcomes.Unauthenticated,
			McpToolTelemetry.Outcomes.ProxyError,
		];

		all.Should().OnlyContain(outcome => string.Equals(outcome, outcome.ToLowerInvariant(), StringComparison.Ordinal));
	}
}
