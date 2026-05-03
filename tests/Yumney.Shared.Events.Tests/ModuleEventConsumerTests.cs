using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Events.Wolverine;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class ModuleEventConsumerTests
{
	[Fact]
	public async Task HandleAsync_NewMessage_InvokesHandlerOnce()
	{
		var handler = Substitute.For<IModuleEventHandler<TestModuleEvent>>();
		var inbox = new NoOpInboxStore();
		var consumer = CreateConsumer(handler, inbox);
		var message = new TestModuleEvent("owner-1");

		await consumer.HandleAsync(message, CancellationToken.None);

		await handler.Received(1).HandleAsync(message, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_DuplicateMessage_SkipsHandler()
	{
		var handler = Substitute.For<IModuleEventHandler<TestModuleEvent>>();
		var inbox = Substitute.For<IInboxStore>();
		inbox.TryMarkProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(false);

		var consumer = CreateConsumer(handler, inbox);
		var message = new TestModuleEvent("owner-1");

		await consumer.HandleAsync(message, CancellationToken.None);

		await handler.DidNotReceive().HandleAsync(Arg.Any<TestModuleEvent>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleAsync_MultipleHandlers_GatesEachIndependently()
	{
		var handlerA = new RecordingHandler();
		var handlerB = new RecordingHandler();

		var inbox = Substitute.For<IInboxStore>();
		inbox.TryMarkProcessedAsync(Arg.Any<Guid>(), Arg.Is<string>(n => n.Contains(nameof(RecordingHandler))), Arg.Any<CancellationToken>())
			.Returns(true, false);

		var services = new ServiceCollection();
		services.AddSingleton<IModuleEventHandler<TestModuleEvent>>(handlerA);
		services.AddSingleton<IModuleEventHandler<TestModuleEvent>>(handlerB);
		var provider = services.BuildServiceProvider();

		var consumer = new ModuleEventConsumer<TestModuleEvent>(
			provider,
			inbox,
			NullLogger<ModuleEventConsumer<TestModuleEvent>>.Instance);

		await consumer.HandleAsync(new TestModuleEvent("owner-1"), CancellationToken.None);

		var totalCalls = handlerA.CallCount + handlerB.CallCount;
		totalCalls.Should().Be(1, "exactly one handler runs when the inbox allows one and blocks the other");
	}

	[Fact]
	public async Task HandleAsync_NoRegisteredHandlers_DoesNothing()
	{
		var inbox = Substitute.For<IInboxStore>();
		var provider = new ServiceCollection().BuildServiceProvider();

		var consumer = new ModuleEventConsumer<TestModuleEvent>(
			provider,
			inbox,
			NullLogger<ModuleEventConsumer<TestModuleEvent>>.Instance);

		await consumer.HandleAsync(new TestModuleEvent("owner-1"), CancellationToken.None);

		await inbox.DidNotReceive().TryMarkProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	private sealed class RecordingHandler : IModuleEventHandler<TestModuleEvent>
	{
		public int CallCount { get; private set; }

		public Task HandleAsync(TestModuleEvent moduleEvent, CancellationToken cancellationToken = default)
		{
			CallCount++;
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
