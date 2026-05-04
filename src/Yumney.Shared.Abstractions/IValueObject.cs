namespace SmartSolutionsLab.Yumney.Shared.Abstractions;

// Marker interface
public interface IValueObject;

public interface IValueObject<TPrimitive> : IValueObject
{
	TPrimitive Value { get; }
}
