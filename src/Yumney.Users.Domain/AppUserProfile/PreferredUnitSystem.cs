using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record PreferredUnitSystem : IValueObject<string>
{
    public const int MaxLength = 20;

    public static readonly PreferredUnitSystem Metric = new("metric");
    public static readonly PreferredUnitSystem Imperial = new("imperial");

    private static readonly string[] AllowedValues = [Metric, Imperial];

    public string Value { get; }

    private PreferredUnitSystem(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
    }

    public static PreferredUnitSystem From(string value)
    {
        Ensure.That(value).IsOneOf(AllowedValues);
        return new(value);
    }

    public static implicit operator string(PreferredUnitSystem obj) => obj.Value;
}
