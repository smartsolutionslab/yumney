using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

public sealed record Quantity(Amount Amount, Unit? Unit) : IValueObject
{
    public static Quantity Of(Amount amount, Unit? unit) => new(amount, unit);

    public static Quantity From(decimal amount, Unit? unit) => new(Amount.From(amount), unit);

    public static Quantity From(decimal amount, string? unit) =>
        new(Amount.From(amount), unit is not null ? Unit.From(unit) : null);

    public static Quantity? FromNullable(Amount? amount, Unit? unit) =>
        amount is not null ? new Quantity(amount, unit) : null;

    public override string ToString() => Unit is not null
        ? $"{Amount} {Unit}"
        : Amount.ToString();
}
