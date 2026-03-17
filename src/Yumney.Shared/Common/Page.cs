using System.Security.Cryptography;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shared.Common;

public sealed record Page
{
    public int Value { get; }

    public Page(int value)
    {
        Value = Ensure.That(value).IsPositive().AndReturn();
    }

    public static Page From(int value) => new(value);

    public int SkipCount(PageSize pageSize) => (Value - 1) * pageSize.Value;
}
