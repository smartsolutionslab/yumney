namespace SmartSolutionsLab.Yumney.Shared.Web;

public interface IModuleHttpClient
{
	Task<TResult> GetOrDefaultAsync<TResult>(
		string url,
		TResult fallback,
		string operation,
		CancellationToken cancellationToken = default);

	Task<TResult?> FindAsync<TResult>(
		string url,
		string operation,
		CancellationToken cancellationToken = default)
		where TResult : class;

	Task PostAsync<TBody>(
		string url,
		TBody body,
		string operation,
		CancellationToken cancellationToken = default);

	Task PutAsync<TBody>(
		string url,
		TBody body,
		string operation,
		CancellationToken cancellationToken = default);
}

public interface IModuleHttpClientFactory
{
	IModuleHttpClient For(string upstreamName);
}
