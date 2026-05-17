using FluentAssertions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class NoOpInboxStoreTests
{
	private readonly NoOpInboxStore store = new();

	[Fact]
	public async Task TryProcessAsync_InvokesHandlerAndReturnsProcessed()
	{
		var invoked = false;

		var outcome = await store.TryProcessAsync(
			Guid.NewGuid(),
			"consumer",
			ct =>
			{
				invoked = true;
				return Task.CompletedTask;
			});

		invoked.Should().BeTrue();
		outcome.Should().Be(InboxOutcome.Processed);
	}

	[Fact]
	public async Task TryProcessAsync_ForwardsCancellationToken()
	{
		using var cts = new CancellationTokenSource();
		CancellationToken seenToken = default;

		await store.TryProcessAsync(
			Guid.NewGuid(),
			"consumer",
			ct =>
			{
				seenToken = ct;
				return Task.CompletedTask;
			},
			cts.Token);

		seenToken.Should().Be(cts.Token);
	}

	[Fact]
	public async Task TryProcessAsync_HandlerThrows_PropagatesException()
	{
		var act = async () => await store.TryProcessAsync(
			Guid.NewGuid(),
			"consumer",
			ct => throw new InvalidOperationException("boom"));

		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
	}
}
