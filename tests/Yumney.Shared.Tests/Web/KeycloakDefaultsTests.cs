using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SmartSolutionsLab.Yumney.Shared.Web;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Web;

public class KeycloakDefaultsTests
{
    [Fact]
    public void GetBaseUrl_WithConnectionString_ReturnsConnectionString()
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            ["ConnectionStrings:keycloak"] = "http://custom-keycloak:9090",
        });

        var result = KeycloakDefaults.GetBaseUrl(config);

        result.Should().Be("http://custom-keycloak:9090");
    }

    [Fact]
    public void GetBaseUrl_WithServiceUrl_ReturnsServiceUrl()
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            ["services:keycloak:http:0"] = "http://service-keycloak:7070",
        });

        var result = KeycloakDefaults.GetBaseUrl(config);

        result.Should().Be("http://service-keycloak:7070");
    }

    [Fact]
    public void GetBaseUrl_NoConfig_ReturnsDefault()
    {
        var config = CreateConfig();

        var result = KeycloakDefaults.GetBaseUrl(config);

        result.Should().Be(KeycloakDefaults.DefaultUrl);
    }

    [Fact]
    public void GetRealm_WithConfig_ReturnsConfigured()
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            ["Keycloak:Realm"] = "custom-realm",
        });

        var result = KeycloakDefaults.GetRealm(config);

        result.Should().Be("custom-realm");
    }

    [Fact]
    public void GetRealm_NoConfig_ReturnsDefault()
    {
        var config = CreateConfig();

        var result = KeycloakDefaults.GetRealm(config);

        result.Should().Be(KeycloakDefaults.DefaultRealm);
    }

    [Fact]
    public void AuthorizationUrl_DefaultConfig_ReturnsCorrectUrl()
    {
        var config = CreateConfig();

        var result = KeycloakDefaults.AuthorizationUrl(config);

        result.Should().Be("http://localhost:8080/realms/yumney/protocol/openid-connect/auth");
    }

    [Fact]
    public void TokenUrl_DefaultConfig_ReturnsCorrectUrl()
    {
        var config = CreateConfig();

        var result = KeycloakDefaults.TokenUrl(config);

        result.Should().Be("http://localhost:8080/realms/yumney/protocol/openid-connect/token");
    }

    [Fact]
    public void RealmUrl_DefaultConfig_ReturnsCorrectUrl()
    {
        var config = CreateConfig();

        var result = KeycloakDefaults.RealmUrl(config);

        result.Should().Be("http://localhost:8080/realms/yumney");
    }

    [Fact]
    public void AuthorizationUrl_CustomConfig_ReturnsCorrectUrl()
    {
        var config = CreateConfig(new Dictionary<string, string?>
        {
            ["ConnectionStrings:keycloak"] = "http://my-keycloak:9090",
            ["Keycloak:Realm"] = "my-realm",
        });

        var result = KeycloakDefaults.AuthorizationUrl(config);

        result.Should().Be("http://my-keycloak:9090/realms/my-realm/protocol/openid-connect/auth");
    }

    private static IConfiguration CreateConfig(Dictionary<string, string?>? values = null)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? [])
            .Build();
    }
}
