using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record RemovalReason : IValueObject<string>
{
	public const int MaxLength = 500;

	public string Value { get; }

	private RemovalReason(string value)
	{
		string validated = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.AndReturn();
		Value = validated.Trim();
	}

	public static RemovalReason From(string value) => new(value);

	public static RemovalReason? FromNullable(string? value) => value.HasValue() ? new RemovalReason(value!) : null;

	public static implicit operator string(RemovalReason obj) => obj.Value;
}
