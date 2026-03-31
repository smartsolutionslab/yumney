namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Marker interface for domain value objects.
/// Value objects are immutable, compared by value, and wrap a primitive type.
/// </summary>
public interface IValueObject<out T>
{
    T Value { get; }
}
