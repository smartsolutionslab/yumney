using SmartSolutionsLab.Yumney.Shared.Events;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

/// <summary>
/// Hand-written fake inbox so tests can drive scope outcomes
/// (ShouldProcess true/false, optional duplicate-race detection) and assert
/// Commit / Rollback ordering without standing up an EF Core context.
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

	public List<TestInboxScope> Scopes { get; } = [];

	public Task<IInboxScope> BeginAsync(Guid messageId, string consumerName, CancellationToken cancellationToken = default)
	{
		var shouldProcess = shouldProcessQueue.Count > 0 ? shouldProcessQueue.Dequeue() : true;
		var scope = new TestInboxScope(messageId, consumerName, shouldProcess, isDuplicate);
		Scopes.Add(scope);
		return Task.FromResult<IInboxScope>(scope);
	}
}

public sealed class TestInboxScope(Guid messageId, string consumerName, bool shouldProcess, Func<Exception, bool> isDuplicate)
	: IInboxScope
{
	public Guid MessageId { get; } = messageId;

	public string ConsumerName { get; } = consumerName;

	public bool ShouldProcess { get; } = shouldProcess;

	public bool Committed { get; private set; }

	public bool RolledBack { get; private set; }

	public bool Disposed { get; private set; }

	public Task CommitAsync(CancellationToken cancellationToken = default)
	{
		Committed = true;
		return Task.CompletedTask;
	}

	public Task RollbackAsync(CancellationToken cancellationToken = default)
	{
		RolledBack = true;
		return Task.CompletedTask;
	}

	public bool IsDuplicateInboxViolation(Exception exception) => isDuplicate(exception);

	public ValueTask DisposeAsync()
	{
		Disposed = true;
		return ValueTask.CompletedTask;
	}
}
