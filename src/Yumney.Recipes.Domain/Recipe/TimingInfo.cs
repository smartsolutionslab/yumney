namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

/// <summary>
/// Composite value object pairing preparation time and cooking time.
/// Either or both components may be null when unknown. The object
/// itself is null when neither value is provided.
/// </summary>
public sealed record TimingInfo
{
	public PreparationTime? Preparation { get; }

	public CookingTime? Cooking { get; }

	private TimingInfo(PreparationTime? preparation, CookingTime? cooking)
	{
		Preparation = preparation;
		Cooking = cooking;
	}

	public static TimingInfo Of(PreparationTime? preparation, CookingTime? cooking) =>
		new(preparation, cooking);

	public static TimingInfo? FromNullable(PreparationTime? preparation, CookingTime? cooking) =>
		preparation is null && cooking is null ? null : new(preparation, cooking);

	public int? TotalMinutes
	{
		get
		{
			var prep = Preparation?.Value ?? 0;
			var cook = Cooking?.Value ?? 0;
			return prep == 0 && cook == 0 ? null : prep + cook;
		}
	}
}
