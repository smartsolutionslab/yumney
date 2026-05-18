using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

public sealed record WeeklyBalanceGoals : IValueObject
{
	public const int MinMeals = 0;
	public const int MaxMeals = 7;

	public int? MinVeggieMeals { get; }

	public int? MaxRedMeatMeals { get; }

	private WeeklyBalanceGoals()
	{
	}

	private WeeklyBalanceGoals(int? minVeggieMeals, int? maxRedMeatMeals)
	{
		if (minVeggieMeals.HasValue)
		{
			Ensure.That(minVeggieMeals.Value).IsInRange(MinMeals, MaxMeals);
		}

		if (maxRedMeatMeals.HasValue)
		{
			Ensure.That(maxRedMeatMeals.Value).IsInRange(MinMeals, MaxMeals);
		}

		MinVeggieMeals = minVeggieMeals;
		MaxRedMeatMeals = maxRedMeatMeals;
	}

	public static WeeklyBalanceGoals From(int? minVeggieMeals, int? maxRedMeatMeals) => new(minVeggieMeals, maxRedMeatMeals);

	public static readonly WeeklyBalanceGoals None = new(null, null);

	public bool IsEmpty => !MinVeggieMeals.HasValue && !MaxRedMeatMeals.HasValue;
}
