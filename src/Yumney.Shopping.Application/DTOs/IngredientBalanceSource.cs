namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

/// <summary>
/// Origin of a balance sheet entry. Tracks whether an item is at home
/// because it was bought (or marked as already-at-home) or because it
/// is on the user's staples list.
/// </summary>
public enum IngredientBalanceSource
{
	/// <summary>Item is at home because of bought / added-as-at-home ledger events.</summary>
	AtHome,

	/// <summary>Item is always considered available because it is on the user's staples list.</summary>
	Staple,
}
