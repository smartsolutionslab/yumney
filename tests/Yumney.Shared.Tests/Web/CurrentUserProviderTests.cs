using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shared.Web.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class CurrentUserProviderTests
{
	[Fact]
	public void UserId_PrefersNameIdentifierClaim()
	{
		var provider = Build(
			new Claim(ClaimTypes.NameIdentifier, "kc-user-1"),
			new Claim(KeycloakClaimTypes.Subject, "fallback"));

		provider.UserId.Should().Be("kc-user-1");
	}

	[Fact]
	public void UserId_FallsBackToKeycloakSubjectClaim()
	{
		var provider = Build(new Claim(KeycloakClaimTypes.Subject, "kc-sub-1"));

		provider.UserId.Should().Be("kc-sub-1");
	}

	[Fact]
	public void UserId_NoHttpContext_ReturnsEmpty()
	{
		BuildWithoutHttpContext().UserId.Should().BeEmpty();
	}

	[Fact]
	public void Email_PrefersEmailClaim()
	{
		var provider = Build(
			new Claim(ClaimTypes.Email, "preferred@example.com"),
			new Claim(KeycloakClaimTypes.Email, "fallback@example.com"));

		provider.Email.Should().Be("preferred@example.com");
	}

	[Fact]
	public void Email_FallsBackToKeycloakEmail()
	{
		var provider = Build(new Claim(KeycloakClaimTypes.Email, "kc@example.com"));

		provider.Email.Should().Be("kc@example.com");
	}

	[Fact]
	public void Email_NoClaim_ReturnsEmpty()
	{
		Build().Email.Should().BeEmpty();
	}

	[Fact]
	public void DisplayName_PrefersPreferredUsernameClaim()
	{
		var provider = Build(
			new Claim(KeycloakClaimTypes.PreferredUsername, "alice"),
			new Claim(ClaimTypes.Name, "Alice Anderson"));

		provider.DisplayName.Should().Be("alice");
	}

	[Fact]
	public void DisplayName_FallsBackToNameClaim()
	{
		var provider = Build(new Claim(ClaimTypes.Name, "Alice Anderson"));

		provider.DisplayName.Should().Be("Alice Anderson");
	}

	[Fact]
	public void Roles_NoClaims_ReturnsEmpty()
	{
		Build().Roles.Should().BeEmpty();
	}

	[Fact]
	public void Roles_MultipleClaims_ReturnsAll()
	{
		var provider = Build(
			new Claim(ClaimTypes.Role, "user"),
			new Claim(ClaimTypes.Role, "admin"));

		provider.Roles.Should().BeEquivalentTo("user", "admin");
	}

	[Fact]
	public void Roles_NoHttpContext_ReturnsEmpty()
	{
		BuildWithoutHttpContext().Roles.Should().BeEmpty();
	}

	[Fact]
	public void IsAuthenticated_AuthenticatedIdentity_ReturnsTrue()
	{
		var provider = Build(new Claim(ClaimTypes.NameIdentifier, "u1"));

		provider.IsAuthenticated.Should().BeTrue();
	}

	[Fact]
	public void IsAuthenticated_NoHttpContext_ReturnsFalse()
	{
		BuildWithoutHttpContext().IsAuthenticated.Should().BeFalse();
	}

	[Fact]
	public void IsInRole_MatchingRole_ReturnsTrue()
	{
		var provider = Build(new Claim(ClaimTypes.Role, "admin"));

		provider.IsInRole("admin").Should().BeTrue();
	}

	[Fact]
	public void IsInRole_NonMatchingRole_ReturnsFalse()
	{
		var provider = Build(new Claim(ClaimTypes.Role, "user"));

		provider.IsInRole("admin").Should().BeFalse();
	}

	[Fact]
	public void IsInRole_NoHttpContext_ReturnsFalse()
	{
		BuildWithoutHttpContext().IsInRole("admin").Should().BeFalse();
	}

	private static CurrentUserProvider Build(params Claim[] claims)
	{
		var identity = new ClaimsIdentity(claims, authenticationType: "Bearer");
		var principal = new ClaimsPrincipal(identity);
		var httpContext = new DefaultHttpContext { User = principal };
		var accessor = Substitute.For<IHttpContextAccessor>();
		accessor.HttpContext.Returns(httpContext);
		return new CurrentUserProvider(accessor);
	}

	private static CurrentUserProvider BuildWithoutHttpContext()
	{
		var accessor = Substitute.For<IHttpContextAccessor>();
		accessor.HttpContext.Returns((HttpContext?)null);
		return new CurrentUserProvider(accessor);
	}
}
