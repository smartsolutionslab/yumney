using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shared.Common;

public sealed record PageSize
{
    public int Value { get; }

    private PageSize(int value)
    {
        Value = Ensure.That(value).IsPositive().AndReturn();
    }

    public static PageSize From(int value) => new(value);
}
