namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger;

public sealed record ItemBalance(string ItemName, decimal OnList, decimal Bought, decimal Consumed, decimal AtHome)
{
    public decimal Remaining => OnList - Bought;
}
