namespace SmartSolutionsLab.Yumney.Shopping.Application;

/// <summary>
/// Runtime toggles for the Shopping module. Bound from configuration section
/// <c>Shopping</c>.
/// </summary>
public sealed class ShoppingOptions
{
	public const string SectionName = "Shopping";

	/// <summary>
	/// Gets or sets a value indicating whether <c>GetShoppingLists</c> and
	/// <c>GetShoppingListById</c> read from the projection tables populated by
	/// the event stream. When <c>false</c>, queries fall back to the relational
	/// write tables — the legacy path. Used as an emergency rollback during the
	/// Phase 4 cutover. Defaults to <c>true</c>.
	/// </summary>
	public bool UseProjectionReadModel { get; set; } = true;
}
