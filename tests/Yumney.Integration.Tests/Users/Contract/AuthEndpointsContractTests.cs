using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;
using Xunit;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Users.Contract;

/// <summary>
/// Contract tests for /api/v1/auth endpoints (Register, ResendVerificationEmail).
/// Both are anonymous. ResendVerificationEmail always returns 200 to prevent email enumeration.
/// </summary>
[Collection(AspireCollection.Name)]
public class AuthEndpointsContractTests(AspireFixture fixture)
{
	private const string Register = "/api/v1/auth/register";
	private const string ResendVerify = "/api/v1/auth/resend-verification-email";
	private const string KnownTestEmail = "test@yumney.dev";

	[Fact]
	public async Task Register_ValidInput_Returns201()
	{
		var client = fixture.UsersApi;
		var uniqueEmail = $"register-{Guid.NewGuid():N}@yumney.dev";

		var response = await client.PostAsJsonAsync(Register, new
		{
			email = uniqueEmail,
			password = "Valid1Pass",
			displayName = "New User",
		});

		response.StatusCode.Should().Be(HttpStatusCode.Created);
	}

	[Fact]
	public async Task Register_InvalidEmailFormat_Returns422()
	{
		var client = fixture.UsersApi;

		var response = await client.PostAsJsonAsync(Register, new
		{
			email = "not-an-email",
			password = "Valid1Pass",
			displayName = "New User",
		});

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task Register_PasswordWithoutUppercase_Returns422()
	{
		var client = fixture.UsersApi;

		var response = await client.PostAsJsonAsync(Register, new
		{
			email = $"noupper-{Guid.NewGuid():N}@yumney.dev",
			password = "allowercase1",
			displayName = "New User",
		});

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task Register_EmptyDisplayName_Returns422()
	{
		var client = fixture.UsersApi;

		var response = await client.PostAsJsonAsync(Register, new
		{
			email = $"nodn-{Guid.NewGuid():N}@yumney.dev",
			password = "Valid1Pass",
			displayName = string.Empty,
		});

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}

	[Fact]
	public async Task Register_ExistingEmail_Returns409()
	{
		var client = fixture.UsersApi;

		var response = await client.PostAsJsonAsync(Register, new
		{
			email = KnownTestEmail,
			password = "Valid1Pass",
			displayName = "Duplicate",
		});

		response.StatusCode.Should().Be(HttpStatusCode.Conflict);
	}

	[Fact]
	public async Task ResendVerification_UnknownEmail_ReturnsOkToPreventEnumeration()
	{
		var client = fixture.UsersApi;

		var response = await client.PostAsJsonAsync(ResendVerify, new
		{
			email = $"nosuch-{Guid.NewGuid():N}@yumney.dev",
		});

		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task ResendVerification_InvalidEmailFormat_Returns422()
	{
		var client = fixture.UsersApi;

		var response = await client.PostAsJsonAsync(ResendVerify, new { email = "not-an-email" });

		response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
	}
}
