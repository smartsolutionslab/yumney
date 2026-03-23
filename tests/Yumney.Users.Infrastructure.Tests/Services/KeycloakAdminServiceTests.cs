using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Tests.Services;

public class KeycloakAdminServiceTests
{
    private readonly IDistributedCache cache = Substitute.For<IDistributedCache>();
    private readonly ILogger<KeycloakAdminService> logger = Substitute.For<ILogger<KeycloakAdminService>>();

    private readonly IOptions<KeycloakOptions> options = Options.Create(new KeycloakOptions
    {
        Realm = "test-realm",
        ClientId = "test-client",
        ClientSecret = "test-secret",
    });

    [Fact]
    public async Task CreateUserAsync_Success_ReturnsKeycloakUserId()
    {
        var keycloakUserId = Guid.NewGuid().ToString();
        var handler = new FakeHttpHandler()
            .WithTokenResponse()
            .WithResponse(
                HttpMethod.Post,
                "/admin/realms/test-realm/users",
                HttpStatusCode.Created,
                headers: new Dictionary<string, string>
                {
                    ["Location"] = $"https://keycloak.test/admin/realms/test-realm/users/{keycloakUserId}",
                });

        var service = CreateService(handler);

        var result = await service.CreateUserAsync(
            new Email("user@test.com"),
            new Password("Password1!"),
            new DisplayName("Test User"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(keycloakUserId);
    }

    [Fact]
    public async Task CreateUserAsync_Conflict_ReturnsEmailAlreadyExists()
    {
        var handler = new FakeHttpHandler()
            .WithTokenResponse()
            .WithResponse(HttpMethod.Post, "/admin/realms/test-realm/users", HttpStatusCode.Conflict);

        var service = CreateService(handler);

        var result = await service.CreateUserAsync(
            new Email("existing@test.com"),
            new Password("Password1!"),
            new DisplayName("Existing User"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RegistrationErrors.EmailAlreadyExists);
    }

    [Fact]
    public async Task CreateUserAsync_TokenFailure_ReturnsIdentityProviderUnavailable()
    {
        var handler = new FakeHttpHandler()
            .WithResponse(HttpMethod.Post, "/realms/test-realm/protocol/openid-connect/token", HttpStatusCode.Unauthorized);

        var service = CreateService(handler);

        var result = await service.CreateUserAsync(
            new Email("user@test.com"),
            new Password("Password1!"),
            new DisplayName("Test User"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(RegistrationErrors.IdentityProviderUnavailable);
    }

    [Fact]
    public async Task FindUserByEmailAsync_UserFound_ReturnsKeycloakUserId()
    {
        var keycloakUserId = Guid.NewGuid().ToString();
        var handler = new FakeHttpHandler()
            .WithTokenResponse()
            .WithJsonResponse(
                HttpMethod.Get,
                "/admin/realms/test-realm/users",
                new[] { new { id = keycloakUserId, email = "user@test.com" } });

        var service = CreateService(handler);

        var result = await service.FindUserByEmailAsync(new Email("user@test.com"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(keycloakUserId);
    }

    [Fact]
    public async Task FindUserByEmailAsync_UserNotFound_ReturnsFailure()
    {
        var handler = new FakeHttpHandler()
            .WithTokenResponse()
            .WithJsonResponse<object[]>(HttpMethod.Get, "/admin/realms/test-realm/users", []);

        var service = CreateService(handler);

        var result = await service.FindUserByEmailAsync(new Email("unknown@test.com"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(VerificationErrors.UserNotFound);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_Success_ReturnsSuccess()
    {
        var handler = new FakeHttpHandler()
            .WithTokenResponse()
            .WithResponse(HttpMethod.Put, "/admin/realms/test-realm/users/", HttpStatusCode.OK);

        var service = CreateService(handler);

        var result = await service.SendVerificationEmailAsync(new KeycloakUserId("user-123"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendVerificationEmailAsync_Failure_ReturnsSendFailed()
    {
        var handler = new FakeHttpHandler()
            .WithTokenResponse()
            .WithResponse(HttpMethod.Put, "/admin/realms/test-realm/users/", HttpStatusCode.InternalServerError);

        var service = CreateService(handler);

        var result = await service.SendVerificationEmailAsync(new KeycloakUserId("user-123"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(VerificationErrors.SendFailed);
    }

    private KeycloakAdminService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://keycloak.test") };
        return new KeycloakAdminService(httpClient, options, cache, logger);
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly List<(HttpMethod Method, string PathPrefix, HttpStatusCode Status, string? JsonBody, Dictionary<string, string>? Headers)> responses = [];

        public FakeHttpHandler WithTokenResponse()
        {
            responses.Add((HttpMethod.Post, "/realms/test-realm/protocol/openid-connect/token",
                HttpStatusCode.OK, JsonSerializer.Serialize(new { access_token = "fake-token" }), null));
            return this;
        }

        public FakeHttpHandler WithResponse(HttpMethod method, string pathPrefix, HttpStatusCode status, Dictionary<string, string>? headers = null)
        {
            responses.Add((method, pathPrefix, status, null, headers));
            return this;
        }

        public FakeHttpHandler WithJsonResponse<T>(HttpMethod method, string pathPrefix, T body)
        {
            responses.Add((method, pathPrefix, HttpStatusCode.OK, JsonSerializer.Serialize(body), null));
            return this;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var match = responses.FirstOrDefault(r =>
                r.Method == request.Method &&
                request.RequestUri!.PathAndQuery.StartsWith(r.PathPrefix, StringComparison.OrdinalIgnoreCase));

            if (match == default)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            var response = new HttpResponseMessage(match.Status);

            if (match.JsonBody is not null)
            {
                response.Content = new StringContent(match.JsonBody, System.Text.Encoding.UTF8, "application/json");
            }

            if (match.Headers is not null)
            {
                foreach (var (key, value) in match.Headers)
                {
                    response.Headers.TryAddWithoutValidation(key, value);
                }
            }

            return Task.FromResult(response);
        }
    }
}
