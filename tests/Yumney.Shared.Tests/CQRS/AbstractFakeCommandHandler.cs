using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public abstract class AbstractFakeCommandHandler : ICommandHandler<AbstractCommand, string>
{
    public abstract Task<string> HandleAsync(AbstractCommand command, CancellationToken cancellationToken = default);
}
