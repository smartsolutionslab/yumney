using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

/// <summary>
/// Snapshot of a recipe identifier captured at the time an activity was
/// recorded. Local to the Users module so it stays decoupled from Recipes.
/// </summary>
public sealed record RecipeIdentifierSnapshot : IValueObject<Guid>
{
	public Guid Value { get; }

	private RecipeIdentifierSnapshot(Guid value)
	{
		Value = Ensure.That(value).IsNotEmpty().AndReturn();
	}

	public static RecipeIdentifierSnapshot From(Guid value) => new(value);

	public static RecipeIdentifierSnapshot New() => new(Guid.CreateVersion7());

	public static RecipeIdentifierSnapshot? FromNullable(Guid? value) =>
		!value.HasValue || value.Value == Guid.Empty ? null : new RecipeIdentifierSnapshot(value.Value);

	public static implicit operator Guid(RecipeIdentifierSnapshot snapshot) => snapshot.Value;
}
