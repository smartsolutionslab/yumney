namespace SmartSolutionsLab.Yumney.Shared.Web;

/// <summary>Rate limit policy names.</summary>
public static class RateLimitPolicies
{
	/// <summary>Strict limit for LLM recipe import (10/min).</summary>
	public const string RecipeImport = "RecipeImport";

	/// <summary>General API rate limit (60/min).</summary>
	public const string GeneralApi = "GeneralApi";

	/// <summary>Strict limit for unauthenticated auth endpoints (10/min/IP) — registration, resend verification, etc.</summary>
	public const string AnonymousAuth = "AnonymousAuth";
}
