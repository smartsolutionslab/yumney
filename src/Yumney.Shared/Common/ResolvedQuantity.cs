namespace SmartSolutionsLab.Yumney.Shared.Common;

public sealed record ResolvedQuantity(decimal Amount, string Unit)
{
    public static readonly ResolvedQuantity OnePiece = new(1, "pc");

    public override string ToString() => $"{Amount} {Unit}";
}
