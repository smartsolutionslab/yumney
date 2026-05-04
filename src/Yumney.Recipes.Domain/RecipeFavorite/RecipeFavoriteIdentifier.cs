using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;

public sealed record RecipeFavoriteIdentifier : IValueObject
{
	public Guid Value { get; }

	private RecipeFavoriteIdentifier(Guid value)
	{
		Value = Ensure.That(value).IsNotEmpty().AndReturn();
	}

	public static RecipeFavoriteIdentifier New() => new(Guid.CreateVersion7());

	public static RecipeFavoriteIdentifier From(Guid value) => new(value);

	public override string ToString() => Value.ToString();
}
