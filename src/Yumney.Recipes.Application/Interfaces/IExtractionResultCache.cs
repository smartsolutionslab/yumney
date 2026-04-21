using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;

/// <summary>
/// Idempotency cache for LLM extraction. Keyed by a hash of the
/// cleaned page text so two requests against the same content
/// (e.g. the same URL re-imported, or the same page hit from
/// different clients) share a single LLM call.
/// </summary>
public interface IExtractionResultCache
{
	/// <summary>
	/// Computes the stable cache key for a piece of cleaned text.
	/// Callers should use this to avoid duplicating the hashing logic.
	/// </summary>
	/// <param name="cleanedText">The cleaned, sanitized page text.</param>
	/// <returns>A stable, provider-agnostic cache key.</returns>
	string ComputeKey(string cleanedText);

	/// <summary>
	/// Returns a cached extraction for <paramref name="key"/>, or null.
	/// </summary>
	/// <param name="key">The key returned by <see cref="ComputeKey"/>.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The cached DTO, or null if none / expired.</returns>
	Task<ExtractedRecipeDto?> GetAsync(string key, CancellationToken cancellationToken = default);

	/// <summary>
	/// Stores a successful extraction under <paramref name="key"/>.
	/// </summary>
	/// <param name="key">The key returned by <see cref="ComputeKey"/>.</param>
	/// <param name="recipe">The extracted recipe to cache.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task representing the store operation.</returns>
	Task SetAsync(string key, ExtractedRecipeDto recipe, CancellationToken cancellationToken = default);
}
