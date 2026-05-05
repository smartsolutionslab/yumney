using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

/// <summary>
/// Hand-written fake inbox so consumer tests can drive ProcessAsync outcomes:
/// queue a per-call <c>shouldProcess</c> sequence (true → run handler, false →
/// return early), and assert how many times handlers were invoked, plus
/// whether they threw. Captures one <see cref="Invocation"/> per call.
/// </summary>
public sealed class TestInboxStore : IInboxStore
{
	private readonly Queue<bool> shouldProcessQueue = new();
	private readonly Func<Exception, bool> isDuplicate;

	public TestInboxStore(IEnumerable<bool>? shouldProcessSequence = null, Func<Exception, bool>? isDuplicate = null)
	{
		if (shouldProcessSequence is not null)
		{
			foreach (var value in shouldProcessSequence)
			{
				shouldProcessQueue.Enqueue(value);
			}
		}

		this.isDuplicate = isDuplicate ?? (_ => false);
	}

	public List<Invocation> Invocations { get; } = [];

	public async Task<bool> ProcessAsync(
		Guid messageId,
		string consumerName,
		Func<Task> handler,
		CancellationToken cancellationToken = default)
	{
		var shouldProcess = shouldProcessQueue.Count > 0 ? shouldProcessQueue.Dequeue() : true;
		var invocation = new Invocation(messageId, consumerName, shouldProcess);
		Invocations.Add(invocation);

		if (!shouldProcess) return false;

		try
		{
			await handler();
			invocation.HandlerCompleted = true;
			return true;
		}
		catch (Exception exception) when (isDuplicate(exception))
		{
			// Race: peer recorded the row first; behave as already-processed.
			invocation.DuplicateRace = true;
			return false;
		}
	}

	public sealed class Invocation(Guid messageId, string consumerName, bool shouldProcess)
	{
		public Guid MessageId { get; } = messageId;

		public string ConsumerName { get; } = consumerName;

		public bool ShouldProcess { get; } = shouldProcess;

		public bool HandlerCompleted { get; set; }

		public bool DuplicateRace { get; set; }
	}
}
