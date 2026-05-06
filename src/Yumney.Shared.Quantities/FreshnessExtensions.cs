namespace SmartSolutionsLab.Yumney.Shared.Quantities;

public static class FreshnessExtensions
{
	public static int Urgency(this Freshness freshness) => freshness switch
	{
		Freshness.CheckIt => 3,
		Freshness.UseSoon => 2,
		Freshness.Fresh => 1,
		_ => 0,
	};

	public static bool IsUrgent(this Freshness freshness)
		=> freshness is Freshness.UseSoon or Freshness.CheckIt;
}
