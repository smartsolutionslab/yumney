using System.Diagnostics.CodeAnalysis;

namespace SmartSolutionsLab.Yumney.Shared.CQRS;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Handler suffix is intentional for CQRS pattern")]
public interface IQueryHandler<in TQuery, TResult>
	where TQuery : IQuery<TResult>
{
	Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
