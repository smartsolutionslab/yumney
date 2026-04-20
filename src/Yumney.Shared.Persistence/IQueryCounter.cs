namespace SmartSolutionsLab.Yumney.Shared.Persistence;

/// <summary>
/// Counts database commands executed within a scope. Used with
/// <see cref="QueryCountingInterceptor"/> to detect N+1 regressions
/// in integration tests and diagnose query hotspots.
/// </summary>
public interface IQueryCounter
{
	int Count { get; }

	void Increment();

	void Reset();
}
