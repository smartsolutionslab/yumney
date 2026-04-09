using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record DietaryProfile : IValueObject
{
    public DietaryType? DietaryType { get; }

    public IReadOnlyList<DietaryRestriction> Restrictions { get; }

    public WeeklyBalanceGoals BalanceGoals { get; }

    public CookingEffortPreference? CookingEffort { get; }

    private DietaryProfile()
    {
        Restrictions = [];
        BalanceGoals = WeeklyBalanceGoals.None;
    }

    private DietaryProfile(
        DietaryType? dietaryType,
        IReadOnlyList<DietaryRestriction> restrictions,
        WeeklyBalanceGoals balanceGoals,
        CookingEffortPreference? cookingEffort)
    {
        DietaryType = dietaryType;
        Restrictions = restrictions;
        BalanceGoals = balanceGoals;
        CookingEffort = cookingEffort;
    }

    public static DietaryProfile From(
        DietaryType? dietaryType,
        IReadOnlyList<DietaryRestriction> restrictions,
        WeeklyBalanceGoals balanceGoals,
        CookingEffortPreference? cookingEffort) =>
        new(dietaryType, restrictions, balanceGoals, cookingEffort);

    public static readonly DietaryProfile Empty = new(null, [], WeeklyBalanceGoals.None, null);

    public bool IsEmpty => DietaryType is null && Restrictions.Count == 0 && BalanceGoals.IsEmpty && CookingEffort is null;
}
