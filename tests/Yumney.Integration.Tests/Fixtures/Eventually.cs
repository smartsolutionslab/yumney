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
	// 30s is a deliberate trade between fast feedback and tolerance for
	// suite-wide load. Wolverine handlers race the polling assert; with the
	// full integration suite running on a cold GitHub Actions runner,
	// RabbitMQ + projection workers can stack up enough that a single
	// check-off event needs >15s to surface in the read model — see
	// issue #606 for the recurrence trail. 30s leaves headroom without
	// making a real failure (a handler bug that never converges) feel
	// sluggish; a converging handler typically wins on the first or second
	// poll, so the timeout is only paid on genuine breakage.
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
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
