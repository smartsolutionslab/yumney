using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class InboxMessageTests
{
	[Fact]
	public void RequiredProperties_AreStamped()
	{
		var messageId = Guid.NewGuid();
		var message = new InboxMessage
		{
			MessageId = messageId,
			ConsumerName = "ShoppingLedger.RecipeDeleted",
		};

		message.MessageId.Should().Be(messageId);
		message.ConsumerName.Should().Be("ShoppingLedger.RecipeDeleted");
	}

	[Fact]
	public void ProcessedAt_DefaultsToUtcNow()
	{
		var message = new InboxMessage
		{
			MessageId = Guid.NewGuid(),
			ConsumerName = "consumer",
		};

		message.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
	}

	[Fact]
	public void ProcessedAt_CanBeOverridden()
	{
		var when = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);

		var message = new InboxMessage
		{
			MessageId = Guid.NewGuid(),
			ConsumerName = "consumer",
			ProcessedAt = when,
		};

		message.ProcessedAt.Should().Be(when);
	}
}
