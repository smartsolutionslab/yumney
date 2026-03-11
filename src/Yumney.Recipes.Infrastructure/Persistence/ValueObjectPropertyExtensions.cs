using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;

internal static class ValueObjectPropertyExtensions
{
    internal static PropertyBuilder<T> ConfigureRequiredStringValueObject<T>(
        this PropertyBuilder<T> builder,
        Func<T, string> toProvider,
        Func<string, T> fromProvider,
        int maxLength)
    {
        return builder
            .HasConversion(v => toProvider(v), v => fromProvider(v))
            .HasMaxLength(maxLength)
            .IsRequired();
    }

    internal static PropertyBuilder<T?> ConfigureNullableStringValueObject<T>(
        this PropertyBuilder<T?> builder,
        Func<T, string> toProvider,
        Func<string?, T?> fromNullable,
        int maxLength)
        where T : class
    {
        return builder
            .HasConversion(v => v != null ? toProvider(v) : null, v => fromNullable(v))
            .HasMaxLength(maxLength);
    }

    internal static PropertyBuilder<T?> ConfigureNullableIntValueObject<T>(
        this PropertyBuilder<T?> builder,
        Func<T, int> toProvider,
        Func<int?, T?> fromNullable)
        where T : class
    {
        return builder
            .HasConversion(v => v != null ? toProvider(v) : (int?)null, v => fromNullable(v));
    }

    internal static PropertyBuilder<T?> ConfigureNullableDecimalValueObject<T>(
        this PropertyBuilder<T?> builder,
        Func<T, decimal> toProvider,
        Func<decimal?, T?> fromNullable)
        where T : class
    {
        return builder
            .HasConversion(v => v != null ? toProvider(v) : (decimal?)null, v => fromNullable(v));
    }

    internal static PropertyBuilder<T> ConfigureRequiredIntValueObject<T>(
        this PropertyBuilder<T> builder,
        Func<T, int> toProvider,
        Func<int, T> fromProvider)
    {
        return builder
            .HasConversion(v => toProvider(v), v => fromProvider(v))
            .IsRequired();
    }
}
