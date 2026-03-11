using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;

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
}
