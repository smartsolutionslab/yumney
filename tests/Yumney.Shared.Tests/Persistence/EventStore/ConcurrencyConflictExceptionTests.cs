using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence.EventStore;

public class ConcurrencyConflictExceptionTests
{
	[Fact]
	public void Ctor_StoresAggregateNameAndId()
	{
		var aggregateId = Guid.NewGuid();

		var exception = new ConcurrencyConflictException("ShoppingList", aggregateId);

		exception.AggregateName.Should().Be("ShoppingList");
		exception.AggregateId.Should().Be(aggregateId);
	}

	[Fact]
	public void Message_IncludesAggregateNameAndId()
	{
		var aggregateId = Guid.NewGuid();

		var exception = new ConcurrencyConflictException("ShoppingList", aggregateId);

		exception.Message.Should().Be($"Concurrent update detected on ShoppingList {aggregateId}.");
	}

	[Fact]
	public void Ctor_PreservesInnerException()
	{
		var inner = new InvalidOperationException("boom");

		var exception = new ConcurrencyConflictException("Recipe", Guid.NewGuid(), inner);

		exception.InnerException.Should().BeSameAs(inner);
	}
}
