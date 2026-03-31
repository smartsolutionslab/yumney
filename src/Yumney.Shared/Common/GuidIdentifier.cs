using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shared.Common;

public abstract record GuidIdentifier
{
    public Guid Value { get; }

    protected GuidIdentifier(Guid value)
    {
        Value = Ensure.That(value).IsNotEmpty().AndReturn();
    }

    public sealed override string ToString() => Value.ToString();
}
