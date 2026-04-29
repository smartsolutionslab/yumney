namespace SmartSolutionsLab.Yumney.Shared.Common;

/// <summary>
/// Cross-module read access to a user's "available ingredients" set —
/// the union of at-home ledger items (Bought − Consumed − Removed &gt; 0)
/// and staples. Returned names are lowercased / trimmed to match
/// case-insensitively against recipe ingredient names.
/// Implementations: Shopping module (in-process) and a HTTP client for
/// callers in other modules (e.g., Recipes' "What Can I Cook?" matcher).
/// </summary>
public interface IIngredientBalanceProvider
{
	Task<IReadOnlySet<string>> GetAvailableIngredientNamesAsync(string ownerId, CancellationToken cancellationToken = default);
}
