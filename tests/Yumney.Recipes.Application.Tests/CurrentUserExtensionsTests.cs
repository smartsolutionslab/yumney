using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests;

public class CurrentUserExtensionsTests
{
	[Fact]
	public void AsOwner_ProjectsUserIdIntoOwnerIdentifier()
	{
		var currentUser = Substitute.For<ICurrentUser>();
		currentUser.UserId.Returns("kc-user-1");

		var owner = currentUser.AsOwner();

		owner.Should().Be(OwnerIdentifier.From("kc-user-1"));
	}
}
