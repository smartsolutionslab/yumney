namespace SmartSolutionsLab.Yumney.Shared.Persistence;

public sealed class QueryCounter : IQueryCounter
{
	private int count;

	public int Count => count;

	public void Increment() => Interlocked.Increment(ref count);

	public void Reset() => Interlocked.Exchange(ref count, 0);
}
