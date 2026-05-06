using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Quantities;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Api;

/// <content>
/// Category-related endpoints (US-083): re-categorize a shopping list item.
/// </content>
public static partial class ShoppingEndpoints
{
	private static void MapCategoryEndpoints(RouteGroupBuilder group)
	{
		group.MapPost("/{identifier:guid}/items/{itemId:guid}/category", ChangeItemCategoryAsync)
			.WithName("ChangeItemCategory")
			.WithTags("Shopping")
			.WithValidation<Requests.ChangeItemCategory>()
			.Produces(StatusCodes.Status204NoContent)
			.ProducesValidationProblem()
			.ProducesProblem(StatusCodes.Status404NotFound);
	}

	private static async Task<IResult> ChangeItemCategoryAsync(
		Guid identifier,
		Guid itemId,
		Requests.ChangeItemCategory request,
		ICommandHandler<ChangeItemCategoryCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var command = new ChangeItemCategoryCommand(
			ShoppingListIdentifier.From(identifier),
			ShoppingListItemIdentifier.From(itemId),
			IngredientCategory.From(request.Category));

		var result = await handler.HandleAsync(command, cancellationToken);
		return result.ToNoContent();
	}
}
