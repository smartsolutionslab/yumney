using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Tests.Services;

public class CurrentUserServiceTests
{
    private readonly IHttpContextAccessor httpContextAccessor = Substitute.For<IHttpContextAccessor>();

    [Fact]
    public void UserId_ReturnsSubClaim_WhenPresent()
    {
        SetupUser(new Claim("sub", "user-123"));

        var sut = new CurrentUserService(httpContextAccessor);

        sut.UserId.Should().Be("user-123");
    }

    [Fact]
    public void UserId_ReturnsEmpty_WhenNoClaims()
    {
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var sut = new CurrentUserService(httpContextAccessor);

        sut.UserId.Should().BeEmpty();
    }

    [Fact]
    public void Email_ReturnsEmailClaim_WhenPresent()
    {
        SetupUser(new Claim(ClaimTypes.Email, "test@example.com"));

        var sut = new CurrentUserService(httpContextAccessor);

        sut.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void IsAuthenticated_ReturnsTrue_WhenUserIsAuthenticated()
    {
        SetupAuthenticatedUser("user-123");

        var sut = new CurrentUserService(httpContextAccessor);

        sut.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_ReturnsFalse_WhenNoHttpContext()
    {
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var sut = new CurrentUserService(httpContextAccessor);

        sut.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void Roles_ReturnsAllRoleClaims()
    {
        SetupUser(
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "user"));

        var sut = new CurrentUserService(httpContextAccessor);

        sut.Roles.Should().BeEquivalentTo(["admin", "user"]);
    }

    [Fact]
    public void IsInRole_ReturnsTrue_WhenUserHasRole()
    {
        SetupAuthenticatedUser("user-123", "admin");

        var sut = new CurrentUserService(httpContextAccessor);

        sut.IsInRole("admin").Should().BeTrue();
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
        var claims = new List<Claim> { new("sub", userId) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        httpContextAccessor.HttpContext.Returns(httpContext);
    }
}
