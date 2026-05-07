using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Users.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Tests.Services;

public class HttpStaplesProviderTests
{
	[Fact]
	public async Task GetStapleNamesAsync_EmptyList_ReturnsEmptySet()
	{
		var provider = CreateProvider([]);

		var result = await provider.GetStapleNamesAsync();

		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetStapleNamesAsync_PopulatedList_ReturnsCaseInsensitiveSet()
	{
		var provider = CreateProvider(["Flour", "Sugar"]);

		var result = await provider.GetStapleNamesAsync();

		result.Contains("flour").Should().BeTrue();
		result.Contains("SUGAR").Should().BeTrue();
	}

	private static HttpStaplesProvider CreateProvider(IReadOnlyList<string> staples)
	{
		var users = Substitute.For<IUsersClient>();
		users.GetMyStaplesAsync(Arg.Any<CancellationToken>()).Returns(staples);
		return new HttpStaplesProvider(users);
	}
}
