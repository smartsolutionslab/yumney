using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public sealed class FakeCommandHandler : ICommandHandler<FakeCommand, string>
{
    public Task<string> HandleAsync(FakeCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult("handled");
}
