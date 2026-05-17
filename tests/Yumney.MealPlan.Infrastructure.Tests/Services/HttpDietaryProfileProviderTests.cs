using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.MealPlan.Application.Interfaces;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure.ExternalServices;
using SmartSolutionsLab.Yumney.Users.Client;
using Xunit;

namespace SmartSolutionsLab.Yumney.MealPlan.Infrastructure.Tests.Services;

public class HttpDietaryProfileProviderTests
{
	private readonly IUsersClient users = Substitute.For<IUsersClient>();

	[Fact]
	public async Task GetAsync_ProfileNull_ReturnsEmpty()
	{
		users.GetMyProfileAsync(Arg.Any<CancellationToken>()).Returns((UsersProfileResponse?)null);

		var snapshot = await new HttpDietaryProfileProvider(users).GetAsync();

		snapshot.Should().Be(DietaryProfileSnapshot.Empty);
	}

	[Fact]
	public async Task GetAsync_DietaryProfileMissing_ReturnsEmpty()
	{
		users.GetMyProfileAsync(Arg.Any<CancellationToken>())
			.Returns(new UsersProfileResponse(DietaryProfile: null));

		var snapshot = await new HttpDietaryProfileProvider(users).GetAsync();

		snapshot.Should().Be(DietaryProfileSnapshot.Empty);
	}

	[Fact]
	public async Task GetAsync_DietaryProfilePresent_ProjectsTypeAndRestrictions()
	{
		users.GetMyProfileAsync(Arg.Any<CancellationToken>())
			.Returns(new UsersProfileResponse(new DietaryProfilePayload("vegan", ["nuts", "gluten"])));

		var snapshot = await new HttpDietaryProfileProvider(users).GetAsync();

		snapshot.DietaryType.Should().Be("vegan");
		snapshot.Restrictions.Should().Equal("nuts", "gluten");
	}

	[Fact]
	public async Task GetAsync_RestrictionsNull_NormalisesToEmptyList()
	{
		users.GetMyProfileAsync(Arg.Any<CancellationToken>())
			.Returns(new UsersProfileResponse(new DietaryProfilePayload("omnivore", Restrictions: null)));

		var snapshot = await new HttpDietaryProfileProvider(users).GetAsync();

		snapshot.DietaryType.Should().Be("omnivore");
		snapshot.Restrictions.Should().BeEmpty();
	}
}
