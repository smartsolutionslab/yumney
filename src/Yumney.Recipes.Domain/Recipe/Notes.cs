using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

/// <summary>
/// Personal cooking notes attached to a recipe (US-120). Multiline; line breaks
/// are preserved, no rich-text. Capped at 2000 chars to keep the column bounded.
/// </summary>
public sealed record Notes : IValueObject<string>
{
	public const int MaxLength = 2000;

	public string Value { get; }

	private Notes(string value)
	{
		Value = Ensure.That(value).IsNotNullOrWhiteSpace().HasMaxLength(MaxLength).AndReturn();
	}

	public static Notes From(string value) => new(value);

	public static Notes? FromNullable(string? value) => value.HasValue() ? new Notes(value!) : null;

	public static implicit operator string(Notes notes) => notes.Value;
}
