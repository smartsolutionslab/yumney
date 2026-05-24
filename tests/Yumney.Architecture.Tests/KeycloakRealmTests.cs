using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Architecture.Tests;

// Locks in the Keycloak realm config that external MCP clients depend on.
// History: 2026-05-24 staging redeploy left Claude.ai unable to complete OAuth
// because the realm had no `yumney-api` client scope — DCR-registered clients
// (Claude.ai, ChatGPT custom GPTs) inherit realm defaults at registration, not
// the per-client audience mapper on yumney-web, so they couldn't request a
// token with the `yumney-api` audience. Runtime-patched via admin API; this
// test fails if that fix isn't carried into the imported realm JSON, which
// would re-break the connector on the next from-scratch RG wipe.
public class KeycloakRealmTests
{
	private const string YumneyApiScope = "yumney-api";
	private const string AudienceMapperProvider = "oidc-audience-mapper";

	private static readonly Lazy<JsonDocument> Realm = new(() =>
	{
		var path = Path.Combine(SolutionRoot.Src, "Yumney.AppHost", "Realms", "yumney-realm.json");
		var json = File.ReadAllText(path);
		return JsonDocument.Parse(json);
	});

	[Fact]
	public void Realm_DefinesYumneyApiClientScope()
	{
		var clientScopes = TryGetArray(Realm.Value.RootElement, "clientScopes");

		clientScopes
			.Should()
			.NotBeNull("`clientScopes` is the only realm-level place to register the yumney-api scope so DCR-registered clients (Claude.ai, ChatGPT) can request it");

		var scope = clientScopes!.Value
			.EnumerateArray()
			.FirstOrDefault(element => element.TryGetProperty("name", out var name) && name.GetString() == YumneyApiScope);

		scope.ValueKind
			.Should()
			.NotBe(
				JsonValueKind.Undefined,
				"a client scope named '{0}' must exist so DCR clients can request `scope=yumney-api` without Keycloak rejecting it as Invalid scopes",
				YumneyApiScope);
	}

	[Fact]
	public void YumneyApiScope_HasAudienceProtocolMapper()
	{
		var scope = FindYumneyApiScope();

		var mappers = TryGetArray(scope, "protocolMappers");
		mappers.Should().NotBeNull("the yumney-api scope must include a protocol mapper that injects the audience into issued tokens");

		var audienceMapper = mappers!.Value
			.EnumerateArray()
			.FirstOrDefault(mapper =>
				mapper.TryGetProperty("protocolMapper", out var provider) && provider.GetString() == AudienceMapperProvider);

		audienceMapper.ValueKind
			.Should()
			.NotBe(
				JsonValueKind.Undefined,
				"an `{0}` is required so tokens issued for this scope carry `aud=yumney-api`, which the module APIs validate",
				AudienceMapperProvider);

		audienceMapper.TryGetProperty("config", out var config).Should().BeTrue();
		config.TryGetProperty("included.client.audience", out var audience).Should().BeTrue();
		audience.GetString()
			.Should()
			.Be(YumneyApiScope, "the audience the mapper injects must match the API client ID");

		config.TryGetProperty("access.token.claim", out var accessTokenClaim).Should().BeTrue();
		accessTokenClaim.GetString()
			.Should()
			.Be("true", "the audience must land in the access token, which is the one the module APIs validate");
	}

	[Fact]
	public void Realm_RegistersYumneyApiAsDefaultOptionalScope()
	{
		var optional = TryGetArray(Realm.Value.RootElement, "defaultOptionalClientScopes");
		optional
			.Should()
			.NotBeNull("`defaultOptionalClientScopes` is what makes DCR-registered clients automatically inherit the yumney-api scope at registration time");

		var present = optional!.Value
			.EnumerateArray()
			.Any(element => element.GetString() == YumneyApiScope);

		present
			.Should()
			.BeTrue(
				"yumney-api must be in defaultOptionalClientScopes so Claude.ai / ChatGPT can request it (current realm: [{0}])",
				string.Join(", ", optional.Value.EnumerateArray().Select(element => element.GetString())));
	}

	private static JsonElement FindYumneyApiScope()
	{
		var clientScopes = TryGetArray(Realm.Value.RootElement, "clientScopes");
		clientScopes.Should().NotBeNull("clientScopes array is required");

		return clientScopes!.Value
			.EnumerateArray()
			.First(element => element.TryGetProperty("name", out var name) && name.GetString() == YumneyApiScope);
	}

	private static JsonElement? TryGetArray(JsonElement parent, string propertyName)
	{
		return parent.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Array
			? value
			: null;
	}
}
