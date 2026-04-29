using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;

public sealed class GetIngredientBalanceQueryHandler(
	IIngredientBalanceReadModelRepository readModel,
	IStaplesProvider staplesProvider,
	ICurrentUser currentUser) : IQueryHandler<GetIngredientBalanceQuery, Result<IngredientBalanceDto>>
{
	public async Task<Result<IngredientBalanceDto>> HandleAsync(GetIngredientBalanceQuery query, CancellationToken cancellationToken = default)
	{
		var ownerId = currentUser.UserId;

		var atHomeTask = readModel.GetAtHomeItemsAsync(ownerId, cancellationToken);
		var staplesTask = staplesProvider.GetStapleNamesAsync(ownerId, cancellationToken);
		await Task.WhenAll(atHomeTask, staplesTask);

		var atHomeItems = atHomeTask.Result;
		var staples = staplesTask.Result;

		var atHomeNames = atHomeItems
			.Select(i => i.ItemName)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		List<IngredientBalanceItemDto> items = [.. atHomeItems];

		foreach (var staple in staples.OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
		{
			if (atHomeNames.Contains(staple)) continue;

			var category = IngredientCategoryResolver.Resolve(staple) ?? IngredientCategory.Other;
			items.Add(new IngredientBalanceItemDto(
				ItemName: staple,
				Quantity: null,
				Unit: null,
				Category: category.Value,
				Source: IngredientBalanceSource.Staple));
		}

		List<IngredientBalanceItemDto> sorted = [.. items
			.OrderBy(i => IngredientCategory.From(i.Category).DisplayOrder)
			.ThenBy(i => i.ItemName, StringComparer.OrdinalIgnoreCase)];

		return new IngredientBalanceDto(sorted);
	}
}
