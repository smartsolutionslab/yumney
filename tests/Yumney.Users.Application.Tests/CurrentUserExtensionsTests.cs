using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Users.Application;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Application.Tests;

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

	[Fact]
	public void AsOwner_TwoCallsForSameUser_AreEqual()
	{
		var currentUser = Substitute.For<ICurrentUser>();
		currentUser.UserId.Returns("kc-user-1");

		currentUser.AsOwner().Should().Be(currentUser.AsOwner());
	}
}
