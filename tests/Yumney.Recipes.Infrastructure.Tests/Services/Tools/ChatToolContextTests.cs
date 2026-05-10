using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class ChatToolContextTests
{
	[Fact]
	public void AppendRecipeMatch_NewIdentifier_AddsMatch()
	{
		var context = new ChatToolContext();
		var id = Guid.NewGuid();

		context.AppendRecipeMatch(id, "Carbonara");

		context.Matches.Should().ContainSingle();
		context.Matches[0].Identifier.Should().Be(id);
		context.Matches[0].Title.Should().Be("Carbonara");
		context.Matches[0].Reason.Should().BeNull();
	}

	[Fact]
	public void AppendRecipeMatch_DuplicateIdentifier_DeduplicatesByIdentifier()
	{
		var context = new ChatToolContext();
		var id = Guid.NewGuid();

		context.AppendRecipeMatch(id, "Carbonara");
		context.AppendRecipeMatch(id, "Carbonara (variant)");

		context.Matches.Should().ContainSingle();
		context.Matches[0].Title.Should().Be("Carbonara");
	}

	[Fact]
	public void AppendRecipeMatch_PreservesInsertionOrder()
	{
		var context = new ChatToolContext();
		var first = Guid.NewGuid();
		var second = Guid.NewGuid();
		var third = Guid.NewGuid();

		context.AppendRecipeMatch(first, "First");
		context.AppendRecipeMatch(second, "Second");
		context.AppendRecipeMatch(third, "Third");

		context.Matches.Select(match => match.Title).Should().ContainInOrder("First", "Second", "Third");
	}

	[Fact]
	public void MarkCookableQuery_FlipsProposeStartCookMode()
	{
		var context = new ChatToolContext();

		context.ProposeStartCookMode.Should().BeFalse();
		context.MarkCookableQuery();
		context.ProposeStartCookMode.Should().BeTrue();
	}

	[Fact]
	public void AppendRecipeMatch_PreservesReasonForFirstAppend()
	{
		var context = new ChatToolContext();
		var id = Guid.NewGuid();

		context.AppendRecipeMatch(id, "Carbonara", reason: "Ready to cook");

		context.Matches[0].Reason.Should().Be("Ready to cook");
	}
}
