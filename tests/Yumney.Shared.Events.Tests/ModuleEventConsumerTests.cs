using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class ModuleEventConsumerTests
{
	[Fact]
	public async Task HandleAsync_NewMessage_InvokesHandlerAndCommitsScope()
	{
		var handler = new RecordingHandler();
		var inbox = new TestInboxStore();
		var consumer = CreateConsumer(handler, inbox);
		var message = new TestModuleEvent("owner-1");

		await consumer.HandleAsync(message, CancellationToken.None);

		handler.CallCount.Should().Be(1);
		inbox.Scopes[0].Committed.Should().BeTrue();
	}

	[Fact]
	public async Task HandleAsync_DuplicateMessage_SkipsHandler()
	{
		var handler = new RecordingHandler();
		var inbox = new TestInboxStore(shouldProcessSequence: [false]);
		var consumer = CreateConsumer(handler, inbox);

		await consumer.HandleAsync(new TestModuleEvent("owner-1"), CancellationToken.None);

		handler.CallCount.Should().Be(0);
		inbox.Scopes[0].Committed.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_HandlerThrows_RollsBackAndPropagates()
	{
		var handler = new RecordingHandler { ThrowOnCall = new InvalidOperationException("boom") };
		var inbox = new TestInboxStore();
		var consumer = CreateConsumer(handler, inbox);

		var act = async () => await consumer.HandleAsync(new TestModuleEvent("owner-1"), CancellationToken.None);

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
		inbox.Scopes[0].RolledBack.Should().BeTrue();
		inbox.Scopes[0].Committed.Should().BeFalse();
	}

	[Fact]
	public async Task HandleAsync_MultipleHandlers_GatesEachIndependently()
	{
		var handlerA = new RecordingHandler();
		var handlerB = new RecordingHandler();
		var inbox = new TestInboxStore(shouldProcessSequence: [true, false]);

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
		inbox.Scopes.Count(scope => scope.Committed).Should().Be(1);
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

		inbox.Scopes.Should().BeEmpty();
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

	private static ModuleEventConsumer<TestModuleEvent> CreateConsumer(
		IModuleEventHandler<TestModuleEvent> handler,
		IInboxStore inbox)
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
