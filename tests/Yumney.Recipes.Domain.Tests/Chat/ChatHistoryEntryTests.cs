using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Chat;

public class ChatHistoryEntryTests
{
	[Fact]
	public void PositionalCtor_StampsRoleAndContent()
	{
		var content = ChatMessageContent.From("Hello");

		var entry = new ChatHistoryEntry(ChatRole.User, content);

		entry.Role.Should().Be(ChatRole.User);
		entry.Content.Should().Be(content);
	}

	[Fact]
	public void Equality_SameRoleAndContent_AreEqual()
	{
		var content = ChatMessageContent.From("Hello");

		var a = new ChatHistoryEntry(ChatRole.Assistant, content);
		var b = new ChatHistoryEntry(ChatRole.Assistant, content);

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}
}
