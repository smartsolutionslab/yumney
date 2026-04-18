using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public sealed class FakeQueryHandler : IQueryHandler<FakeQuery, int>
{
	public Task<int> HandleAsync(FakeQuery query, CancellationToken cancellationToken = default)
		=> Task.FromResult(42);
}
