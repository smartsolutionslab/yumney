using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record ShoppingListItemIdentifier : IValueObject
{
	public Guid Value { get; }

	private ShoppingListItemIdentifier(Guid value)
	{
		Value = Ensure.That(value).IsNotEmpty().AndReturn();
	}

	public static ShoppingListItemIdentifier New() => From(Guid.CreateVersion7());

	public static ShoppingListItemIdentifier From(Guid value) => new(value);

	public override string ToString() => Value.ToString();
}
