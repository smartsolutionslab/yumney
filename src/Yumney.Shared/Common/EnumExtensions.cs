namespace SmartSolutionsLab.Yumney.Shared.Common;

public static class EnumExtensions
{
#pragma warning disable SA1313
    public static TEnum? ParseNullable<TEnum>(this TEnum _, string? value)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) ? result : null;
    }
}
