using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;
using SmartSolutionsLab.Yumney.Shared.Guards;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Chat;

public class ChatMessageContentTests
{
	[Fact]
	public void From_TrimsSurroundingWhitespace()
	{
		var content = ChatMessageContent.From("  hello  ");

		content.Value.Should().Be("hello");
	}

	[Fact]
	public void From_AtMaxLength_IsAccepted()
	{
		var content = ChatMessageContent.From(new string('x', ChatMessageContent.MaxLength));

		content.Value.Length.Should().Be(ChatMessageContent.MaxLength);
	}

	[Fact]
	public void From_BeyondMaxLength_Throws()
	{
		var act = () => ChatMessageContent.From(new string('x', ChatMessageContent.MaxLength + 1));

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_Empty_Throws()
	{
		var act = () => ChatMessageContent.From(string.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_Whitespace_Throws()
	{
		var act = () => ChatMessageContent.From("   ");

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void ImplicitConversion_ToString_YieldsValue()
	{
		var content = ChatMessageContent.From("hi");
		string raw = content;

		raw.Should().Be("hi");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var a = ChatMessageContent.From("hi");
		var b = ChatMessageContent.From("hi");

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}
}
