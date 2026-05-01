using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class HttpIngredientBalanceProviderTests
{
	[Fact]
	public async Task GetAvailableIngredientsAsync_EmptyResponse_ReturnsEmptyDictionary()
	{
		var provider = CreateProvider("""{ "items": [] }""");

		var result = await provider.GetAvailableIngredientsAsync();

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_NullItems_ReturnsEmptyDictionary()
	{
		var provider = CreateProvider("""{ "items": null }""");

		var result = await provider.GetAvailableIngredientsAsync();

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_DeserializesEnumAsString()
	{
		var provider = CreateProvider("""
            {
              "items": [
                { "itemName": "Milk", "freshness": "UseSoon" },
                { "itemName": "Salt", "freshness": "NotTracked" }
              ]
            }
            """);

		var result = await provider.GetAvailableIngredientsAsync();

		result["Milk"].Should().Be(Freshness.UseSoon);
		result["Salt"].Should().Be(Freshness.NotTracked);
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_LookupIsCaseInsensitive()
	{
		var provider = CreateProvider("""{ "items": [ { "itemName": "Milk", "freshness": "Fresh" } ] }""");

		var result = await provider.GetAvailableIngredientsAsync();

		result.ContainsKey("milk").Should().BeTrue();
		result.ContainsKey("MILK").Should().BeTrue();
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_TrimsWhitespaceFromNames()
	{
		var provider = CreateProvider("""{ "items": [ { "itemName": "  Milk  ", "freshness": "Fresh" } ] }""");

		var result = await provider.GetAvailableIngredientsAsync();

		result.Should().ContainSingle().Which.Key.Should().Be("Milk");
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_BlankName_Skipped()
	{
		var provider = CreateProvider("""
            {
              "items": [
                { "itemName": "   ", "freshness": "Fresh" },
                { "itemName": "Eggs", "freshness": "Fresh" }
              ]
            }
            """);

		var result = await provider.GetAvailableIngredientsAsync();

		result.Should().ContainSingle().Which.Key.Should().Be("Eggs");
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_DuplicateNameMostUrgentWins()
	{
		// Same name with different freshness (e.g. "milk in liters" UseSoon and
		// "milk in ml" Fresh). The matcher needs the urgent one to surface.
		var provider = CreateProvider("""
            {
              "items": [
                { "itemName": "Milk", "freshness": "Fresh" },
                { "itemName": "milk", "freshness": "UseSoon" },
                { "itemName": "MILK", "freshness": "NotTracked" }
              ]
            }
            """);

		var result = await provider.GetAvailableIngredientsAsync();

		result.Should().ContainSingle().Which.Value.Should().Be(Freshness.UseSoon);
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_DuplicateNameOrderIndependent()
	{
		// Same scenario but reversed order — most-urgent first, then less urgent.
		var provider = CreateProvider("""
            {
              "items": [
                { "itemName": "Chicken", "freshness": "CheckIt" },
                { "itemName": "Chicken", "freshness": "Fresh" }
              ]
            }
            """);

		var result = await provider.GetAvailableIngredientsAsync();

		result["Chicken"].Should().Be(Freshness.CheckIt);
	}

	[Fact]
	public async Task GetAvailableIngredientsAsync_HttpError_PropagatesException()
	{
		var provider = CreateProvider(string.Empty, HttpStatusCode.InternalServerError);

		var act = () => provider.GetAvailableIngredientsAsync();

		await act.Should().ThrowAsync<HttpRequestException>();
	}

	private static HttpIngredientBalanceProvider CreateProvider(string responseBody, HttpStatusCode status = HttpStatusCode.OK)
	{
		var handler = new StubHttpMessageHandler(responseBody, status);
		var client = new HttpClient(handler) { BaseAddress = new Uri("http://shopping-api") };

		var factory = Substitute.For<IHttpClientFactory>();
		factory.CreateClient("shopping-api").Returns(client);

		return new HttpIngredientBalanceProvider(factory);
	}

	private sealed class StubHttpMessageHandler(string body, HttpStatusCode status) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var response = new HttpResponseMessage(status)
			{
				Content = new StringContent(body, Encoding.UTF8, "application/json"),
			};
			return Task.FromResult(response);
		}
	}
}
