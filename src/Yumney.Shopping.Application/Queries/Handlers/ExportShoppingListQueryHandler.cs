using System.Globalization;
using System.Text;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;

/// <summary>
/// Exports the shopping list as formatted text grouped by category.
/// Only includes unbought items. Does not change any item state.
/// </summary>
public sealed class ExportShoppingListQueryHandler(IShoppingLedgerReadModelRepository readModel, ICurrentUser currentUser)
	: IQueryHandler<ExportShoppingListQuery, Result<string>>
{
#pragma warning disable SA1311
	private static readonly Dictionary<string, string> categoryEmojis = new()
	{
		["produce"] = "\U0001F966",
		["dairy"] = "\U0001F95B",
		["meat-fish"] = "\U0001F969",
		["bakery"] = "\U0001F35E",
		["frozen"] = "\u2744\uFE0F",
		["beverages"] = "\U0001F964",
		["pantry"] = "\U0001F3E0",
		["household"] = "\U0001F9F9",
		["other"] = "\U0001F4E6",
	};

	private static readonly Dictionary<string, string> categoryLabels = new()
	{
		["produce"] = "Produce",
		["dairy"] = "Dairy",
		["meat-fish"] = "Meat & Fish",
		["bakery"] = "Bakery",
		["frozen"] = "Frozen",
		["beverages"] = "Beverages",
		["pantry"] = "Pantry",
		["household"] = "Household",
		["other"] = "Other",
	};
#pragma warning restore SA1311

	public async Task<Result<string>> HandleAsync(ExportShoppingListQuery query, CancellationToken cancellationToken = default)
	{
		var list = await readModel.GetByOwnerAsync(currentUser.AsOwner(), cancellationToken: cancellationToken);

		var openItems = list.Items.Where(item => !item.IsBought).ToList();
		if (openItems.Count == 0) return string.Empty;

		var grouped = openItems
			.GroupBy(item => item.Category)
			.OrderBy(group => IngredientCategory.From(group.Key).DisplayOrder);

		var stringBuilder = new StringBuilder();

		foreach (var group in grouped)
		{
			var emoji = categoryEmojis.GetValueOrDefault(group.Key, "\U0001F4E6");
			var label = categoryLabels.GetValueOrDefault(group.Key, "Other");
			stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"{emoji} {label}:");

			foreach (var item in group)
			{
				var qty = QuantityRounder.RoundUp(item.TotalQuantity, item.Unit);
				var unitSuffix = item.Unit is not null ? $" {item.Unit}" : string.Empty;
				stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"  \u2610 {qty.DisplayQuantity}{unitSuffix} {item.ItemName}");
			}

			stringBuilder.AppendLine();
		}

		return stringBuilder.ToString().TrimEnd();
	}
}
