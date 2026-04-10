using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record DietaryType : IValueObject<string>
{
    public const int MaxLength = 20;

    public static readonly DietaryType Omnivore = new("omnivore");
    public static readonly DietaryType Vegetarian = new("vegetarian");
    public static readonly DietaryType Vegan = new("vegan");
    public static readonly DietaryType Pescatarian = new("pescatarian");
    public static readonly DietaryType Flexitarian = new("flexitarian");

    private static readonly string[] AllowedValues =
        [Omnivore, Vegetarian, Vegan, Pescatarian, Flexitarian];

    public string Value { get; }

    private DietaryType(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
    }

    public static DietaryType From(string value)
    {
        Ensure.That(value).IsOneOf(AllowedValues);
        return new(value);
    }

    public static implicit operator string(DietaryType obj) => obj.Value;
}
