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
///
/// The proper structural fix is a per-event readiness signal (something like
/// <c>GET /api/v1/shopping-lists/{id}?waitForEvent={eventId}</c> that blocks
/// until the projection has consumed that event). That would eliminate
/// polling entirely. Until that lands, this helper minimises flake rate via:
/// (a) a generous CI timeout, (b) jitter on the poll interval so parallel
/// tests don't thundering-herd the API at the same instants, and
/// (c) diagnostic info on timeout that lets us tell "projection is slow but
/// converging" from "assertion never starts passing" in the CI logs.
/// </summary>
public static class Eventually
{
	// Local default sits at 30s — fast enough for inner-loop iteration on a
	// warm machine, generous enough that a converging handler always wins.
	//
	// On CI the same suite shares a runner with the full backend test matrix
	// plus a cold AppHost boot, so the first writes can serialise behind
	// Wolverine + RabbitMQ start-up. Set to 90s there — past attempts at 60s
	// still caught flakes on a cold runner (issue #606), and polling
	// short-circuits on success so a healthy projection still completes in
	// well under a second.
	// Random 0..MaxJitterMs added on top of every poll interval so parallel
	// tests under [Collection(AspireCollection.Name)] don't all hit the API
	// at the same 100ms boundary. Removes a small but real source of
	// contention on a single CI runner.
	private const int MaxJitterMs = 50;

	private static readonly TimeSpan LocalTimeout = TimeSpan.FromSeconds(30);
	private static readonly TimeSpan CiTimeout = TimeSpan.FromSeconds(90);
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
		DateTime? firstFailureAt = null;
		var attempts = 0;
		var jitter = new Random();

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
				firstFailureAt ??= DateTime.UtcNow;
				var jitterMs = jitter.Next(MaxJitterMs);
				await Task.Delay(interval + TimeSpan.FromMilliseconds(jitterMs));
			}
			catch (Exception ex)
			{
				lastError = ex;
				break;
			}
		}

		var elapsed = DateTime.UtcNow - start;

		// Time-to-first-failure helps triage: near-zero means the projection
		// never converged in the window; near `elapsed` means the assertion
		// flipped from passing to failing partway through (race with concurrent
		// state mutation — usually a test-design bug, not projection lag).
		var firstFailureElapsed = firstFailureAt is null ? "n/a" : $"{(firstFailureAt.Value - start).TotalSeconds:F2}s";
		throw new TimeoutException(
			$"Eventually.AssertAsync timed out after {elapsed.TotalSeconds:F1}s "
			+ $"and {attempts} attempts (configured timeout {effectiveTimeout.TotalSeconds:F0}s, "
			+ $"CI={IsCi}, time-to-first-failure={firstFailureElapsed}). "
			+ $"Last assertion error: {lastError?.Message}",
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
