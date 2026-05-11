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
	// Local default sits at 30s — fast enough for inner-loop iteration on a
	// warm machine, generous enough that a converging handler always wins.
	//
	// On CI the same suite shares a runner with the full backend test matrix
	// plus a cold AppHost boot, so the first writes can serialise behind
	// Wolverine + RabbitMQ start-up. We double to 60s there to keep the
	// recurrence trail under issue #606 from flaking PRs that are otherwise
	// green. A genuinely broken handler still surfaces inside the window
	// because polling probes every 100 ms.
	private static readonly TimeSpan LocalTimeout = TimeSpan.FromSeconds(30);
	private static readonly TimeSpan CiTimeout = TimeSpan.FromSeconds(60);
	private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(100);

	private static readonly bool IsCi = IsRunningOnCi();

	private static TimeSpan DefaultTimeout => IsCi ? CiTimeout : LocalTimeout;

	public static async Task AssertAsync(
		Func<Task> assertion,
		TimeSpan? timeout = null,
		TimeSpan? pollInterval = null)
	{
		var effectiveTimeout = timeout ?? DefaultTimeout;
		var start = DateTime.UtcNow;
		var deadline = start + effectiveTimeout;
		var interval = pollInterval ?? DefaultPollInterval;
		Exception? lastError = null;
		var attempts = 0;

		while (true)
		{
			attempts++;
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

		var elapsed = DateTime.UtcNow - start;
		throw new TimeoutException(
			$"Eventually.AssertAsync timed out after {elapsed.TotalSeconds:F1}s "
			+ $"and {attempts} attempts (configured timeout {effectiveTimeout.TotalSeconds:F0}s, "
			+ $"CI={IsCi}). Last assertion error: {lastError?.Message}",
			lastError);
	}

	private static bool IsRunningOnCi()
	{
		// Both GitHub Actions and most other runners set CI=true. Treat any
		// truthy CI env var as the trigger so the longer timeout applies
		// uniformly across runner flavours.
		var ciFlag = Environment.GetEnvironmentVariable("CI");
		return !string.IsNullOrEmpty(ciFlag) && ciFlag.Equals("true", StringComparison.OrdinalIgnoreCase);
	}
}
