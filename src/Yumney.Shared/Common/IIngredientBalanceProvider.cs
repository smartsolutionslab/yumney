namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Cross-module read access to a user's "available ingredients" — the union
/// of at-home ledger items (Bought − Consumed − Removed &gt; 0) and staples,
/// keyed by the lowercased / trimmed name and tagged with freshness so
/// downstream callers (e.g. recipe matching) can rank perishables first.
/// Staples and pantry-class items always come back as
/// <see cref="Freshness.NotTracked"/>.
/// </summary>
public interface IIngredientBalanceProvider
{
	Task<IReadOnlyDictionary<string, Freshness>> GetAvailableIngredientsAsync(string ownerId, CancellationToken cancellationToken = default);
}
