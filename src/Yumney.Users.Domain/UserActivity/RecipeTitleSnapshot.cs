using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

/// <summary>
/// Snapshot of a recipe title captured at the time an activity was recorded.
/// Local to the Users module so it stays decoupled from Recipes.Domain.
/// </summary>
public sealed record RecipeTitleSnapshot : IValueObject<string>
{
	public const int MaxLength = 200;

	public string Value { get; }

	private RecipeTitleSnapshot(string value)
	{
		Value = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.AndReturn();
	}

	public static RecipeTitleSnapshot From(string value) => new(value);

	public static RecipeTitleSnapshot? FromNullable(string? value) =>
		string.IsNullOrWhiteSpace(value) ? null : new RecipeTitleSnapshot(value);

	public static implicit operator string(RecipeTitleSnapshot snapshot) => snapshot.Value;
}
