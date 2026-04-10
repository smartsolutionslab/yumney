using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record CookingEffortPreference : IValueObject<string>
{
    public const int MaxLength = 25;

    public static readonly CookingEffortPreference QuickWeekdays = new("quick-weekdays");
    public static readonly CookingEffortPreference Balanced = new("balanced");
    public static readonly CookingEffortPreference ElaborateWeekends = new("elaborate-weekends");

    private static readonly string[] AllowedValues = [QuickWeekdays, Balanced, ElaborateWeekends];

    public string Value { get; }

    private CookingEffortPreference(string value)
    {
        Value = Ensure.That(value)
            .IsNotNullOrWhiteSpace()
            .HasMaxLength(MaxLength)
            .AndReturn();
    }

    public static CookingEffortPreference From(string value)
    {
        Ensure.That(value).IsOneOf(AllowedValues);
        return new(value);
    }

    public static implicit operator string(CookingEffortPreference obj) => obj.Value;
}
