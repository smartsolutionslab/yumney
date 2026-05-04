using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class IntegrationEventConsumerTests
{
	[Fact]
	public async Task HandleAsync_NewMessage_InvokesHandlerAndCommitsScope()
	{
		var handler = new RecordingHandler();
		var inbox = new TestInboxStore();
		var consumer = CreateConsumer(handler, inbox);
		var message = new TestEvent();

		await consumer.HandleAsync(message, CancellationToken.None);

		handler.CallCount.Should().Be(1);
		inbox.Scopes.Should().HaveCount(1);
		inbox.Scopes[0].Committed.Should().BeTrue();
		inbox.Scopes[0].RolledBack.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_DuplicateMessage_SkipsHandlerWithoutCommit()
	{
		var handler = new RecordingHandler();
		var inbox = new TestInboxStore(shouldProcessSequence: [false]);
		var consumer = CreateConsumer(handler, inbox);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		handler.CallCount.Should().Be(0);
		inbox.Scopes[0].Committed.Should().BeFalse();
		inbox.Scopes[0].RolledBack.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_HandlerThrows_RollsBackAndPropagates()
	{
		var handler = new RecordingHandler { ThrowOnCall = new InvalidOperationException("boom") };
		var inbox = new TestInboxStore();
		var consumer = CreateConsumer(handler, inbox);

		var act = async () => await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
		inbox.Scopes[0].Committed.Should().BeFalse();
		inbox.Scopes[0].RolledBack.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_DuplicateRaceOnCommit_RollsBackAndSwallows()
	{
		var raceException = new InvalidOperationException("unique violation");
		var handler = new RecordingHandler { ThrowOnCall = raceException };
		var inbox = new TestInboxStore(isDuplicate: ex => ReferenceEquals(ex, raceException));
		var consumer = CreateConsumer(handler, inbox);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		inbox.Scopes[0].Committed.Should().BeFalse();
		inbox.Scopes[0].RolledBack.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_MultipleHandlers_GatesEachIndependently()
	{
		var handlerA = new RecordingHandler();
		var handlerB = new RecordingHandler();
		var inbox = new TestInboxStore(shouldProcessSequence: [true, false]);

		var services = new ServiceCollection();
		services.AddSingleton<IIntegrationEventHandler<TestEvent>>(handlerA);
		services.AddSingleton<IIntegrationEventHandler<TestEvent>>(handlerB);
		var provider = services.BuildServiceProvider();

		var consumer = new IntegrationEventConsumer<TestEvent>(
			provider,
			inbox,
			NullLogger<IntegrationEventConsumer<TestEvent>>.Instance);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		(handlerA.CallCount + handlerB.CallCount).Should().Be(1);
		inbox.Scopes.Should().HaveCount(2);
		inbox.Scopes.Count(scope => scope.Committed).Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_NoRegisteredHandlers_DoesNothing()
	{
		var inbox = new TestInboxStore();
		var provider = new ServiceCollection().BuildServiceProvider();

		var consumer = new IntegrationEventConsumer<TestEvent>(
			provider,
			inbox,
			NullLogger<IntegrationEventConsumer<TestEvent>>.Instance);

		await consumer.HandleAsync(new TestEvent(), CancellationToken.None);

		inbox.Scopes.Should().BeEmpty();
	}

	[Fact]
	public async Task NoOpInboxStore_AlwaysReturnsProcessableScope()
	{
		var inbox = new NoOpInboxStore();

		await using var first = await inbox.BeginAsync(Guid.NewGuid(), "consumer");
		await using var second = await inbox.BeginAsync(Guid.NewGuid(), "consumer");

		first.ShouldProcess.Should().BeTrue();
		second.ShouldProcess.Should().BeTrue();
	}

	private sealed class RecordingHandler : IIntegrationEventHandler<TestEvent>
	{
		public int CallCount { get; private set; }

		public Exception? ThrowOnCall { get; init; }

		public Task HandleAsync(TestEvent integrationEvent, CancellationToken cancellationToken = default)
		{
			CallCount++;
			if (ThrowOnCall is not null) throw ThrowOnCall;
			return Task.CompletedTask;
		}
	}

	private static IntegrationEventConsumer<TestEvent> CreateConsumer(
		IIntegrationEventHandler<TestEvent> handler,
		IInboxStore inbox)
	{
		var services = new ServiceCollection();
		services.AddSingleton(handler);
		var provider = services.BuildServiceProvider();

		return new IntegrationEventConsumer<TestEvent>(
			provider,
			inbox,
			NullLogger<IntegrationEventConsumer<TestEvent>>.Instance);
	}

	public sealed record TestEvent : IntegrationEvent;
}
