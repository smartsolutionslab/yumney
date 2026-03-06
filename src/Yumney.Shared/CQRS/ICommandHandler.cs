using System.Diagnostics.CodeAnalysis;

namespace Yumney.Shared.CQRS;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Handler suffix is intentional for CQRS pattern")]
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
