using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

#pragma warning disable SA1311 // editorconfig requires camelCase for private fields
public sealed record PreferredUnitSystem : IValueObject<string>
{
    public const int MaxLength = 20;

#pragma warning disable SA1202 // allowedValues must initialize before the public static instances
    private static readonly string[] allowedValues = ["metric", "imperial"];

    public static readonly PreferredUnitSystem Metric = new("metric");
    public static readonly PreferredUnitSystem Imperial = new("imperial");
#pragma warning restore SA1202

    public string Value { get; }

    private PreferredUnitSystem(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .IsOneOf(allowedValues)
            .AndReturn();
    }

    public static PreferredUnitSystem From(string value) => new(value);

    public static implicit operator string(PreferredUnitSystem obj) => obj.Value;
}
#pragma warning restore SA1311
