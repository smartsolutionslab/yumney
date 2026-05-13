using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class ModuleEventConsumerTests
{
	[Fact]
	public async Task HandleAsync_NewMessage_InvokesHandlerAndRecordsProcessedOutcome()
	{
		var handler = new RecordingHandler();
		var inbox = new TestInboxStore();
		var consumer = CreateConsumer(handler, inbox);

		await consumer.HandleAsync(new TestModuleEvent("owner-1"), CancellationToken.None);

		handler.CallCount.Should().Be(1);
		inbox.Invocations[0].Outcome.Should().Be(InboxOutcome.Processed);
		inbox.Invocations[0].HandlerInvoked.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_AlreadyProcessed_SkipsHandler()
	{
		var handler = new RecordingHandler();
		var inbox = new TestInboxStore(outcomeSequence: [InboxOutcome.AlreadyProcessed]);
		var consumer = CreateConsumer(handler, inbox);

		await consumer.HandleAsync(new TestModuleEvent("owner-1"), CancellationToken.None);

		handler.CallCount.Should().Be(0);
		inbox.Invocations[0].HandlerInvoked.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_HandlerThrows_PropagatesAfterStoreRollsBack()
	{
		var handler = new RecordingHandler { ThrowOnCall = new InvalidOperationException("boom") };
		var inbox = new TestInboxStore();
		var consumer = CreateConsumer(handler, inbox);

		var act = async () => await consumer.HandleAsync(new TestModuleEvent("owner-1"), CancellationToken.None);

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
		inbox.Invocations[0].HandlerInvoked.Should().BeTrue();
		inbox.Invocations[0].HandlerThrew.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_MultipleHandlers_GatesEachIndependently()
	{
		var handlerA = new RecordingHandler();
		var handlerB = new RecordingHandler();
		var inbox = new TestInboxStore(outcomeSequence: [InboxOutcome.Processed, InboxOutcome.AlreadyProcessed]);

		var services = new ServiceCollection();
		services.AddSingleton<IModuleEventHandler<TestModuleEvent>>(handlerA);
		services.AddSingleton<IModuleEventHandler<TestModuleEvent>>(handlerB);
		var provider = services.BuildServiceProvider();

		var consumer = new ModuleEventConsumer<TestModuleEvent>(
			provider,
			inbox,
			NullLogger<ModuleEventConsumer<TestModuleEvent>>.Instance);

		await consumer.HandleAsync(new TestModuleEvent("owner-1"), CancellationToken.None);

		(handlerA.CallCount + handlerB.CallCount).Should().Be(1);
		inbox.Invocations.Count(invocation => invocation.HandlerInvoked).Should().Be(1);
	}

	[Fact]
	public async Task HandleAsync_NoRegisteredHandlers_DoesNothing()
	{
		var inbox = new TestInboxStore();
		var provider = new ServiceCollection().BuildServiceProvider();

		var consumer = new ModuleEventConsumer<TestModuleEvent>(
			provider,
			inbox,
			NullLogger<ModuleEventConsumer<TestModuleEvent>>.Instance);

		await consumer.HandleAsync(new TestModuleEvent("owner-1"), CancellationToken.None);

		inbox.Invocations.Should().BeEmpty();
	}

	private sealed class RecordingHandler : IModuleEventHandler<TestModuleEvent>
	{
		public int CallCount { get; private set; }

		public Exception? ThrowOnCall { get; init; }

		public Task HandleAsync(TestModuleEvent moduleEvent, CancellationToken cancellationToken = default)
		{
			CallCount++;
			if (ThrowOnCall is not null) throw ThrowOnCall;
			return Task.CompletedTask;
		}
	}

	private static ModuleEventConsumer<TestModuleEvent> CreateConsumer(IModuleEventHandler<TestModuleEvent> handler, IInboxStore inbox)
	{
		var services = new ServiceCollection();
		services.AddSingleton(handler);
		var provider = services.BuildServiceProvider();

		return new ModuleEventConsumer<TestModuleEvent>(
			provider,
			inbox,
			NullLogger<ModuleEventConsumer<TestModuleEvent>>.Instance);
	}

	public sealed record TestModuleEvent(string OwnerId) : ModuleEvent(OwnerId);
}
