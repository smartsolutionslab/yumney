using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Web.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Tests.Services;

public class CurrentUserProviderTests
{
	private readonly IHttpContextAccessor httpContextAccessor = Substitute.For<IHttpContextAccessor>();

	[Fact]
	public void UserId_ReturnsSubClaim_WhenPresent()
	{
		SetupUser(new Claim("sub", "user-123"));

		var provider = new CurrentUserProvider(httpContextAccessor);

		provider.UserId.Should().Be("user-123");
	}

	[Fact]
	public void UserId_ReturnsEmpty_WhenNoClaims()
	{
		httpContextAccessor.HttpContext.Returns((HttpContext?)null);

		var provider = new CurrentUserProvider(httpContextAccessor);

		provider.UserId.Should().BeEmpty();
	}

	[Fact]
	public void Email_ReturnsEmailClaim_WhenPresent()
	{
		SetupUser(new Claim(ClaimTypes.Email, "test@example.com"));

		var provider = new CurrentUserProvider(httpContextAccessor);

		provider.Email.Should().Be("test@example.com");
	}

	[Fact]
	public void IsAuthenticated_ReturnsTrue_WhenUserIsAuthenticated()
	{
		SetupAuthenticatedUser("user-123");

		var provider = new CurrentUserProvider(httpContextAccessor);

		provider.IsAuthenticated.Should().BeTrue();
	}

	[Fact]
	public void IsAuthenticated_ReturnsFalse_WhenNoHttpContext()
	{
		httpContextAccessor.HttpContext.Returns((HttpContext?)null);

		var provider = new CurrentUserProvider(httpContextAccessor);

		provider.IsAuthenticated.Should().BeFalse();
	}

	[Fact]
	public void Roles_ReturnsAllRoleClaims()
	{
		SetupUser(
			new Claim(ClaimTypes.Role, "admin"),
			new Claim(ClaimTypes.Role, "user"));

		var provider = new CurrentUserProvider(httpContextAccessor);

		provider.Roles.Should().BeEquivalentTo(["admin", "user"]);
	}

	[Fact]
	public void IsInRole_ReturnsTrue_WhenUserHasRole()
	{
		SetupAuthenticatedUser("user-123", "admin");

		var provider = new CurrentUserProvider(httpContextAccessor);

		provider.IsInRole("admin").Should().BeTrue();
	}

	private void SetupUser(params Claim[] claims)
	{
		var identity = new ClaimsIdentity(claims);
		var principal = new ClaimsPrincipal(identity);
		var httpContext = new DefaultHttpContext { User = principal };
		httpContextAccessor.HttpContext.Returns(httpContext);
	}

	private void SetupAuthenticatedUser(string userId, params string[] roles)
	{
		List<Claim> claims = [new("sub", userId)];
		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
		var identity = new ClaimsIdentity(claims, "TestAuth");
		var principal = new ClaimsPrincipal(identity);
		var httpContext = new DefaultHttpContext { User = principal };
		httpContextAccessor.HttpContext.Returns(httpContext);
	}
}
