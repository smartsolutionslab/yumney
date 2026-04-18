using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shared.Common;

public sealed record PageSize
{
	public const int MaxPageSize = 100;

	public int Value { get; }

	private PageSize(int value)
	{
		Value = Ensure.That(value).IsInRange(1, MaxPageSize).AndReturn();
	}

	public static PageSize From(int value) => new(value);
}
