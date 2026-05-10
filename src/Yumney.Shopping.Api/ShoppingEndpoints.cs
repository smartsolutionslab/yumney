using Microsoft.AspNetCore.Mvc;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shared.Web.Capabilities;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Api;

/// <summary>
/// Maps the Shopping module's HTTP endpoints. Split into partial files per
/// concern (lists, items, category) so each file stays under the 300-line cap.
/// </summary>
public static partial class ShoppingEndpoints
{
	public static IEndpointRouteBuilder MapShoppingEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/shopping-lists");
		MapCategoryEndpoints(group);

		group.MapPost("/", Create)
			.WithName("CreateShoppingList")
			.WithTags("Shopping")
			.WithValidation<Requests.CreateShoppingList>()
			.Produces<ShoppingListDetailDto>(StatusCodes.Status201Created)
			.ProducesValidationProblem();

		static async Task<IResult> Create(
			Requests.CreateShoppingList request,
			ICommandHandler<CreateShoppingListCommand, Result<ShoppingListDetailDto>> handler,
			CancellationToken cancellationToken)
		{
			var (title, items, recipeReference) = request.ToValueObjects();
			var command = new CreateShoppingListCommand(title, items, recipeReference);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToCreated($"/api/v1/shopping-lists/{result.Value?.Identifier}");
		}

		group.MapPost("/from-recipes", CreateFromRecipes)
			.WithName("CreateShoppingListFromRecipes")
			.WithTags("Shopping")
			.WithValidation<Requests.CreateFromRecipes>()
			.WithCapability(
				name: "create_shopping_list_from_recipes",
				description: "Create a new shopping list sourced from one or more recipes ('make a shopping list for spaghetti and risotto').",
				surfaces: CapabilitySurface.Chat | CapabilitySurface.Mcp)
			.Produces<ShoppingListDetailDto>(StatusCodes.Status201Created)
			.ProducesValidationProblem();

		static async Task<IResult> CreateFromRecipes(
			Requests.CreateFromRecipes request,
			ICommandHandler<CreateShoppingListFromRecipesCommand, Result<ShoppingListDetailDto>> handler,
			CancellationToken cancellationToken)
		{
			var (title, recipes) = request.ToValueObjects();
			var command = new CreateShoppingListFromRecipesCommand(title, recipes);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToCreated($"/api/v1/shopping-lists/{result.Value?.Identifier}");
		}

		group.MapGet("/", GetAll)
			.WithName("GetShoppingLists")
			.WithTags("Shopping")
			.Produces<PagedResult<ShoppingListSummaryDto>>();

		static async Task<IResult> GetAll(
			IQueryHandler<GetShoppingListsQuery, Result<PagedResult<ShoppingListSummaryDto>>> handler,
			int page = PagingOptions.DefaultPage,
			int pageSize = PagingOptions.DefaultPageSize,
			string sortBy = "Date",
			SortDirection sortDirection = SortDirection.Descending,
			CancellationToken cancellationToken = default)
		{
			var query = new GetShoppingListsQuery(
				PagingOptions.From(page, pageSize),
				SortingOptions<ShoppingListSortField>.Parse(sortBy, sortDirection, ShoppingListSortField.Date));

			var result = await handler.HandleAsync(query, cancellationToken);
			return result.ToOk();
		}

		group.MapGet("/{identifier:guid}", GetById)
			.WithName("GetShoppingListById")
			.WithTags("Shopping")
			.Produces<ShoppingListDetailDto>()
			.ProducesProblem(StatusCodes.Status404NotFound);

		static async Task<IResult> GetById(
			Guid identifier,
			IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListDetailDto>> handler,
			CancellationToken cancellationToken)
		{
			var query = new GetShoppingListByIdQuery(ShoppingListIdentifier.From(identifier));
			var result = await handler.HandleAsync(query, cancellationToken);

			return result.ToOk();
		}

		group.MapPut("/{identifier:guid}/items/{itemId:guid}/check", CheckOffItem)
			.WithName("CheckOffItem")
			.WithTags("Shopping")
			.Produces(StatusCodes.Status204NoContent)
			.ProducesProblem(StatusCodes.Status404NotFound);

		static async Task<IResult> CheckOffItem(
			Guid identifier,
			Guid itemId,
			Requests.CheckOffItem request,
			ICommandHandler<CheckOffItemCommand, Result> handler,
			CancellationToken cancellationToken)
		{
			var command = new CheckOffItemCommand(
				ShoppingListIdentifier.From(identifier),
				ShoppingListItemIdentifier.From(itemId),
				request.IsChecked);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToNoContent();
		}

		group.MapPut("/{identifier:guid}/check-all", CheckOffAllItems)
			.WithName("CheckOffAllItems")
			.WithTags("Shopping")
			.Produces(StatusCodes.Status204NoContent)
			.ProducesProblem(StatusCodes.Status404NotFound);

		static async Task<IResult> CheckOffAllItems(
			Guid identifier,
			Requests.CheckOffItem request,
			ICommandHandler<CheckOffAllItemsCommand, Result> handler,
			CancellationToken cancellationToken)
		{
			var command = new CheckOffAllItemsCommand(ShoppingListIdentifier.From(identifier), request.IsChecked);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToNoContent();
		}

		group.MapPost("/items", AddManualItem)
			.WithName("AddManualItem")
			.WithTags("Shopping")
			.WithValidation<Requests.AddManualItem>()
			.Produces<AddedItemDto>(StatusCodes.Status201Created)
			.ProducesProblem(StatusCodes.Status400BadRequest);

		static async Task<IResult> AddManualItem(
			Requests.AddManualItem request,
			ICommandHandler<AddManualItemCommand, Result<AddedItemDto>> handler,
			CancellationToken cancellationToken)
		{
			var (itemName, quantity, source) = request;
			var command = new AddManualItemCommand(itemName, quantity, source);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.IsSuccess
				? Results.Created($"/shopping-lists/items/{result.Value.LedgerIdentifier}", result.Value)
				: result.ToOk();
		}

		group.MapDelete("/items", RemoveItem)
			.WithName("RemoveShoppingItem")
			.WithTags("Shopping")
			.WithValidation<Requests.RemoveItem>()
			.Produces(StatusCodes.Status204NoContent)
			.ProducesProblem(StatusCodes.Status400BadRequest);

		static async Task<IResult> RemoveItem(
			[FromBody] Requests.RemoveItem request,
			ICommandHandler<RemoveShoppingItemCommand, Result> handler,
			CancellationToken cancellationToken)
		{
			var (itemName, quantity, reason) = request;
			var command = new RemoveShoppingItemCommand(itemName, quantity, reason);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToNoContent();
		}

		group.MapGet("/merged", GetMerged)
			.WithName("GetMergedShoppingList")
			.WithTags("Shopping")
			.WithCapability(
				name: "get_merged_shopping_list",
				description: "Fetch the user's merged active shopping list across all open lists ('what's on my shopping list?', 'do I still need eggs?').",
				surfaces: CapabilitySurface.All)
			.Produces<MergedShoppingListDto>();

		static async Task<IResult> GetMerged(
			IQueryHandler<GetMergedShoppingListQuery, Result<MergedShoppingListDto>> handler,
			CancellationToken cancellationToken,
			bool includePastBought = false)
		{
			var result = await handler.HandleAsync(new GetMergedShoppingListQuery(includePastBought), cancellationToken);
			return result.ToOk();
		}

		group.MapPost("/shopping-mode/start", StartShoppingMode)
			.WithName("StartShoppingMode")
			.WithTags("Shopping")
			.Produces(StatusCodes.Status204NoContent);

		static async Task<IResult> StartShoppingMode(
			ICommandHandler<StartShoppingModeCommand, Result> handler,
			CancellationToken cancellationToken)
		{
			var result = await handler.HandleAsync(new StartShoppingModeCommand(), cancellationToken);
			return result.ToNoContent();
		}

		group.MapPost("/shopping-mode/end", EndShoppingMode)
			.WithName("EndShoppingMode")
			.WithTags("Shopping")
			.Produces(StatusCodes.Status204NoContent);

		static async Task<IResult> EndShoppingMode(
			Requests.EndShoppingMode request,
			ICommandHandler<EndShoppingModeCommand, Result> handler,
			CancellationToken cancellationToken)
		{
			var command = new EndShoppingModeCommand(request.AcceptPendingChanges);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToNoContent();
		}

		group.MapPost("/items/freeze", MarkAsFrozen)
			.WithName("MarkItemAsFrozen")
			.WithTags("Shopping")
			.WithValidation<Requests.MarkAsFrozen>()
			.Produces(StatusCodes.Status204NoContent)
			.ProducesValidationProblem();

		static async Task<IResult> MarkAsFrozen(
			Requests.MarkAsFrozen request,
			ICommandHandler<MarkAsFrozenCommand, Result> handler,
			CancellationToken cancellationToken)
		{
			var (itemName, unit) = request.ToValueObjects();
			var command = new MarkAsFrozenCommand(itemName, unit);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToNoContent();
		}

		group.MapGet("/balance", GetBalance)
			.WithName("GetIngredientBalance")
			.WithTags("Shopping")
			.Produces<IngredientBalanceDto>();

		static async Task<IResult> GetBalance(
			IQueryHandler<GetIngredientBalanceQuery, Result<IngredientBalanceDto>> handler,
			CancellationToken cancellationToken)
		{
			var result = await handler.HandleAsync(new GetIngredientBalanceQuery(), cancellationToken);
			return result.ToOk();
		}

		group.MapGet("/export", Export)
			.WithName("ExportShoppingList")
			.WithTags("Shopping")
			.Produces<string>(contentType: MediaTypes.TextPlain);

		static async Task<IResult> Export(
			IQueryHandler<ExportShoppingListQuery, Result<string>> handler,
			CancellationToken cancellationToken)
		{
			var result = await handler.HandleAsync(new ExportShoppingListQuery(), cancellationToken);

			if (result.IsFailure) return result.ToOk();
			return Results.Text(result.Value, MediaTypes.TextPlain);
		}

		return app;
	}
}
