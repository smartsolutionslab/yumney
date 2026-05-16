using System.Security.Claims;
using System.Threading.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using SmartSolutionsLab.Yumney.Mcp.Server.RateLimit;
using Xunit;

namespace SmartSolutionsLab.Yumney.Mcp.Server.Tests.RateLimit;

public class McpRateLimitTests
{
	[Fact]
	public void PartitionKey_WithSubjectClaim_ReturnsSubject()
	{
		var context = new DefaultHttpContext
		{
			User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-123")], authenticationType: "test")),
		};

		McpRateLimit.PartitionKey(context).Should().Be("user-123");
	}

	[Fact]
	public void PartitionKey_WithoutSubjectClaim_FallsBackToRemoteIp()
	{
		var context = new DefaultHttpContext();
		context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.42");

		McpRateLimit.PartitionKey(context).Should().Be("10.0.0.42");
	}

	[Fact]
	public void PartitionKey_WithoutSubjectOrIp_FallsBackToAnonymous()
	{
		var context = new DefaultHttpContext();

		McpRateLimit.PartitionKey(context).Should().Be("anonymous");
	}

	[Fact]
	public void PolicyName_IsMcp()
	{
		McpRateLimit.PolicyName.Should().Be("Mcp");
	}

	[Fact]
	public void PermitLimit_IsBoundedConservatively()
	{
		// Sanity check: starting ceiling should be > 0 and < a value that
		// would defeat the purpose of having a limit at all.
		McpRateLimit.PermitLimit.Should().BeInRange(10, 200);
	}

	[Fact]
	public void Window_IsAtLeastOneSecond()
	{
		McpRateLimit.Window.Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(1));
	}

	[Fact]
	public void AddMcpPolicy_E2EMode_RegistersWithoutThrowing()
	{
		var options = new RateLimiterOptions();

		Action act = () => options.AddMcpPolicy(isE2ETests: true);

		act.Should().NotThrow();
	}

	[Fact]
	public void AddMcpPolicy_E2EMode_SetsOnRejectedCallback()
	{
		var options = new RateLimiterOptions();

		options.AddMcpPolicy(isE2ETests: true);

		options.OnRejected.Should().NotBeNull("the 429 response shape is meaningful for LLM back-off and must always be wired");
	}
}
