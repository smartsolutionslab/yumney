using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Chat;

public class ChatRoleTests
{
	[Fact]
	public void User_StaticAccessor_HasUserValue()
	{
		ChatRole.User.Value.Should().Be("user");
	}

	[Fact]
	public void Assistant_StaticAccessor_HasAssistantValue()
	{
		ChatRole.Assistant.Value.Should().Be("assistant");
	}

	[Theory]
	[InlineData("user")]
	[InlineData("assistant")]
	[InlineData("USER")]
	[InlineData("Assistant")]
	public void From_KnownRole_ProducesLowercaseValue(string raw)
	{
		var role = ChatRole.From(raw);

		role.Value.Should().Be(raw.ToLowerInvariant());
	}

	[Theory]
	[InlineData("system")]
	[InlineData("bot")]
	[InlineData("nonsense")]
	public void From_UnknownRole_Throws(string raw)
	{
		var act = () => ChatRole.From(raw);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_Empty_Throws()
	{
		var act = () => ChatRole.From(string.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void ImplicitConversion_ToString_YieldsValue()
	{
		string raw = ChatRole.User;

		raw.Should().Be("user");
	}
}
