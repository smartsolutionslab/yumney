using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

/// <summary>
/// Hand-written fake inbox so tests can drive outcomes
/// (Processed / AlreadyProcessed / DuplicateRace) and observe whether the
/// handler delegate ran, without standing up an EF Core context.
/// </summary>
public sealed class TestInboxStore : IInboxStore
{
	private readonly Queue<InboxOutcome> outcomeQueue = new();

	public TestInboxStore(IEnumerable<InboxOutcome>? outcomeSequence = null)
	{
		if (outcomeSequence is not null)
		{
			foreach (var value in outcomeSequence)
			{
				outcomeQueue.Enqueue(value);
			}
		}
	}

	public List<TestInboxInvocation> Invocations { get; } = [];

	public async Task<InboxOutcome> TryProcessAsync(
		Guid messageId,
		string consumerName,
		Func<CancellationToken, Task> handler,
		CancellationToken cancellationToken = default)
	{
		var outcome = outcomeQueue.Count > 0 ? outcomeQueue.Dequeue() : InboxOutcome.Processed;
		var invocation = new TestInboxInvocation(messageId, consumerName, outcome);
		Invocations.Add(invocation);

		// Processed and DuplicateRace both invoke the handler; the latter
		// only differs in that the commit fails on the way out (modeled here
		// by flipping the outcome — the handler still ran). AlreadyProcessed
		// short-circuits before the handler.
		if (outcome != InboxOutcome.AlreadyProcessed)
		{
			invocation.HandlerInvoked = true;
			try
			{
				await handler(cancellationToken);
			}
			catch
			{
				invocation.HandlerThrew = true;
				throw;
			}
		}

		return outcome;
	}
}

public sealed class TestInboxInvocation(Guid messageId, string consumerName, InboxOutcome outcome)
{
	public Guid MessageId { get; } = messageId;

	public string ConsumerName { get; } = consumerName;

	public InboxOutcome Outcome { get; } = outcome;

	public bool HandlerInvoked { get; set; }

	public bool HandlerThrew { get; set; }
}
