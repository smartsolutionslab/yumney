namespace SmartSolutionsLab.Yumney.Shared.Events;

public interface IBusEvent
{
	Guid EventIdentifier { get; }

	DateTime OccurredOn { get; }
}
