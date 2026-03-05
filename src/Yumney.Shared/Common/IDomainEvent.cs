using MediatR;

namespace Yumney.Shared.Common;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
