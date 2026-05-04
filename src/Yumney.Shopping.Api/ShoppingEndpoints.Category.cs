using FluentValidation;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Requests = SmartSolutionsLab.Yumney.Shopping.Api.Requests;

namespace SmartSolutionsLab.Yumney.Shopping.Api;

public static partial class ShoppingEndpoints
{
	private static void MapCategoryEndpoints(RouteGroupBuilder group)
	{
		group.MapPost("/{identifier:guid}/items/{itemId:guid}/category", ChangeItemCategory)
			.WithName("ChangeItemCategory")
			.WithTags("Shopping")
			.Produces(StatusCodes.Status204NoContent)
			.ProducesValidationProblem()
			.ProducesProblem(StatusCodes.Status404NotFound);
	}

	private static async Task<IResult> ChangeItemCategory(
		Guid identifier,
		Guid itemId,
		Requests.ChangeItemCategory request,
		IValidator<Requests.ChangeItemCategory> validator,
		ICommandHandler<ChangeItemCategoryCommand, Result> handler,
		CancellationToken cancellationToken)
	{
		var validation = await validator.ValidateAsync(request, cancellationToken);
		if (validation.HasFailed()) return validation.ToValidationProblem();

		var command = new ChangeItemCategoryCommand(
			ShoppingListIdentifier.From(identifier),
			ShoppingListItemIdentifier.From(itemId),
			IngredientCategory.From(request.Category));

		var result = await handler.HandleAsync(command, cancellationToken);
		return result.ToNoContent();
	}
}
