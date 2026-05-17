using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence.EventStore;

public class StoredEventTests
{
	[Fact]
	public void Properties_AreMutable_ToSupportEfMaterialization()
	{
		var id = Guid.NewGuid();
		var aggregateId = Guid.NewGuid();
		var occurredAt = new DateTime(2026, 5, 17, 10, 0, 0, DateTimeKind.Utc);

		var stored = new StoredEvent
		{
			Id = id,
			AggregateId = aggregateId,
			EventType = "RecipeImported",
			EventData = "{\"step\":3}",
			Version = 7,
			OccurredAt = occurredAt,
		};

		stored.Id.Should().Be(id);
		stored.AggregateId.Should().Be(aggregateId);
		stored.EventType.Should().Be("RecipeImported");
		stored.EventData.Should().Be("{\"step\":3}");
		stored.Version.Should().Be(7);
		stored.OccurredAt.Should().Be(occurredAt);
	}
}
