using System;
using System.Threading.Tasks;

namespace SmartSolutionsLab.Yumney.Integration.Tests.Fixtures;

/// <summary>
/// Polling-retry helper for assertions that must wait for an eventually-
/// consistent read model to catch up.
///
/// The Shopping module publishes integration events through Wolverine/RabbitMQ;
/// the projection handler that updates the read model runs on a worker, so a
/// test that does write → immediate read can race the handler. Wrapping the
/// read+assert block in <see cref="AssertAsync"/> makes the test honest about
/// the async boundary without changing production code.
/// </summary>
public static class Eventually
{
	// 15s is a deliberate trade between fast feedback and tolerance for
	// suite-wide load. Wolverine handlers race the polling assert; with the
	// full integration suite running, RabbitMQ + projection workers can stack
	// up enough that a single check-off event needs more than 5s to surface
	// in the read model. 15s leaves headroom without making real failures
	// (handler bugs that never converge) feel sluggish.
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
	private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(100);

	public static async Task AssertAsync(
		Func<Task> assertion,
		TimeSpan? timeout = null,
		TimeSpan? pollInterval = null)
	{
		var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
		var interval = pollInterval ?? DefaultPollInterval;
		Exception? lastError = null;

		while (true)
		{
			try
			{
				await assertion();
				return;
			}
			catch (Exception ex) when (DateTime.UtcNow < deadline)
			{
				lastError = ex;
				await Task.Delay(interval);
			}
			catch (Exception ex)
			{
				lastError = ex;
				break;
			}
		}

		throw lastError!;
	}
}
