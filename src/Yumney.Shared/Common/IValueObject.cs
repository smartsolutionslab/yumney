namespace SmartSolutionsLab.Yumney.Shared.Common;

// Marker interface
public interface IValueObject;

public interface IValueObject<TPrimitive> : IValueObject
{
    TPrimitive Value { get; }
}
