using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Users.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class HttpDietaryProfileProviderTests
{
	[Fact]
	public async Task GetAsync_NullProfileResponse_ReturnsEmpty()
	{
		var provider = CreateProvider(profile: null);

		var result = await provider.GetAsync();

		result.Should().Be(DietaryProfileSnapshot.Empty);
	}

	[Fact]
	public async Task GetAsync_NullDietaryProfile_ReturnsEmpty()
	{
		var provider = CreateProvider(new UsersProfileResponse(null));

		var result = await provider.GetAsync();

		result.Should().Be(DietaryProfileSnapshot.Empty);
	}

	[Fact]
	public async Task GetAsync_PopulatedProfile_MapsFields()
	{
		var provider = CreateProvider(new UsersProfileResponse(
			new DietaryProfilePayload("vegan", ["nuts", "soy"])));

		var result = await provider.GetAsync();

		result.DietaryType.Should().Be("vegan");
		result.Restrictions.Should().BeEquivalentTo("nuts", "soy");
	}

	[Fact]
	public async Task GetAsync_NullRestrictions_ProjectsToEmptyList()
	{
		var provider = CreateProvider(new UsersProfileResponse(
			new DietaryProfilePayload("vegetarian", null)));

		var result = await provider.GetAsync();

		result.Restrictions.Should().BeEmpty();
	}

	private static HttpDietaryProfileProvider CreateProvider(UsersProfileResponse? profile)
	{
		var users = Substitute.For<IUsersClient>();
		users.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns(profile);
		return new HttpDietaryProfileProvider(users);
	}
}
