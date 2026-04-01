using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Persistence;

public class DomainEventDispatchInterceptorTests
{
    private readonly IDomainEventDispatcher dispatcher = Substitute.For<IDomainEventDispatcher>();
    private readonly DomainEventDispatchInterceptor interceptor;

    public DomainEventDispatchInterceptorTests()
    {
        interceptor = new DomainEventDispatchInterceptor(dispatcher);
    }

    [Fact]
    public async Task SavedChangesAsync_WithDomainEvents_DispatchesEvents()
    {
        await using var context = CreateContext();
        var aggregate = new TestAggregate { Name = "Test" };
        aggregate.RaiseSomething();
        context.TestEntities.Add(aggregate);

        await context.SaveChangesAsync();

        await dispatcher.Received(1).DispatchAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Count() == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_NoDomainEvents_DoesNotDispatch()
    {
        await using var context = CreateContext();
        var aggregate = new TestAggregate { Name = "Test" };
        context.TestEntities.Add(aggregate);

        await context.SaveChangesAsync();

        await dispatcher.DidNotReceive().DispatchAsync(
            Arg.Any<IEnumerable<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavedChangesAsync_ClearsEventsAfterDispatch()
    {
        await using var context = CreateContext();
        var aggregate = new TestAggregate { Name = "Test" };
        aggregate.RaiseSomething();
        context.TestEntities.Add(aggregate);

        await context.SaveChangesAsync();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SavedChangesAsync_MultipleEntities_DispatchesAllEvents()
    {
        await using var context = CreateContext();
        var aggregate1 = new TestAggregate { Name = "First" };
        var aggregate2 = new TestAggregate { Name = "Second" };
        aggregate1.RaiseSomething();
        aggregate2.RaiseSomething();
        aggregate2.RaiseSomething();
        context.TestEntities.Add(aggregate1);
        context.TestEntities.Add(aggregate2);

        await context.SaveChangesAsync();

        await dispatcher.Received(1).DispatchAsync(
            Arg.Is<IEnumerable<IDomainEvent>>(events => events.Count() == 3),
            Arg.Any<CancellationToken>());
    }

    private TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        return new TestDbContext(options);
    }

    private sealed record TestEvent() : DomainEvent;

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestAggregate()
        {
            Id = Guid.NewGuid();
        }

        public void RaiseSomething()
        {
            AddDomainEvent(new TestEvent());
        }
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestAggregate> TestEntities => Set<TestAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestAggregate>(b =>
            {
                b.HasKey(e => e.Id);
                b.Ignore(e => e.DomainEvents);
            });
        }
    }
}
