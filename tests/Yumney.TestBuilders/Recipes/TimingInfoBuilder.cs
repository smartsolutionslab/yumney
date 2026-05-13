using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.TestBuilders.Recipes;

/// <summary>
/// Composite VO builder. <see cref="TimingInfo"/> wraps optional preparation
/// and cooking times; defaults to both components present so the resulting
/// instance is non-null. Use <c>Without*</c> to clear an individual component.
/// </summary>
public sealed class TimingInfoBuilder
{
	private PreparationTime? preparation = PreparationTime.From(15);
	private CookingTime? cooking = CookingTime.From(30);

	public static TimingInfoBuilder A() => new();

	public TimingInfoBuilder WithPreparationMinutes(int minutes)
	{
		preparation = PreparationTime.From(minutes);
		return this;
	}

	public TimingInfoBuilder WithCookingMinutes(int minutes)
	{
		cooking = CookingTime.From(minutes);
		return this;
	}

	public TimingInfoBuilder WithoutPreparation()
	{
		preparation = null;
		return this;
	}

	public TimingInfoBuilder WithoutCooking()
	{
		cooking = null;
		return this;
	}

	public TimingInfo Build() => TimingInfo.Of(preparation, cooking);

	public static implicit operator TimingInfo(TimingInfoBuilder builder) => builder.Build();
}
