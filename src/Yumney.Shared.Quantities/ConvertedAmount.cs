namespace SmartSolutionsLab.Yumney.Shared.Quantities;

/// <summary>
/// A conversion result — already smart-rounded for cooking. Exact source
/// values are not preserved here because callers always want a
/// display-ready number; the original metric/imperial value is one
/// inverse-conversion away when needed.
/// </summary>
/// <param name="Amount">Numeric amount, smart-rounded for kitchen use (no <c>1.7834 oz</c>).</param>
/// <param name="Unit">Target unit suitable for direct display, lower-cased canonical form (<c>oz</c>, <c>fl oz</c>, <c>ml</c>, …). Returned verbatim when no rule applies (count units, unknown tokens).</param>
public sealed record ConvertedAmount(decimal Amount, string Unit);
