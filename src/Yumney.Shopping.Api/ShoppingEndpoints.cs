using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using Requests = SmartSolutionsLab.Yumney.Shopping.Api.Requests;

namespace SmartSolutionsLab.Yumney.Shopping.Api;

public static class ShoppingEndpoints
{
	public static IEndpointRouteBuilder MapShoppingEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/shopping-lists");

		group.MapPost("/", Create)
			.WithName("CreateShoppingList")
			.WithTags("Shopping")
			.Produces<ShoppingListDetailDto>(StatusCodes.Status201Created)
			.ProducesValidationProblem();

		static async Task<IResult> Create(
			Requests.CreateShoppingList request,
			IValidator<Requests.CreateShoppingList> validator,
			ICommandHandler<CreateShoppingListCommand, Result<ShoppingListDetailDto>> handler,
			CancellationToken cancellationToken)
		{
			var validation = await validator.ValidateAsync(request, cancellationToken);
			if (validation.HasFailed()) return validation.ToValidationProblem();

			var (title, items, recipeReference) = request.ToValueObjects();
			var command = new CreateShoppingListCommand(title, items, recipeReference);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToCreated($"/api/v1/shopping-lists/{result.Value?.Identifier}");
		}

		group.MapPost("/from-recipes", CreateFromRecipes)
			.WithName("CreateShoppingListFromRecipes")
			.WithTags("Shopping")
			.Produces<ShoppingListDetailDto>(StatusCodes.Status201Created)
			.ProducesValidationProblem();

		static async Task<IResult> CreateFromRecipes(
			Requests.CreateFromRecipes request,
			IValidator<Requests.CreateFromRecipes> validator,
			ICommandHandler<CreateShoppingListFromRecipesCommand, Result<ShoppingListDetailDto>> handler,
			CancellationToken cancellationToken)
		{
			var validation = await validator.ValidateAsync(request, cancellationToken);
			if (validation.HasFailed()) return validation.ToValidationProblem();

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
			.Produces<AddedItemDto>(StatusCodes.Status201Created)
			.ProducesProblem(StatusCodes.Status400BadRequest);

		static async Task<IResult> AddManualItem(
			Requests.AddManualItem request,
			IValidator<Requests.AddManualItem> validator,
			ICommandHandler<AddManualItemCommand, Result<AddedItemDto>> handler,
			CancellationToken cancellationToken)
		{
			var validation = await validator.ValidateAsync(request, cancellationToken);
			if (validation.HasFailed()) return validation.ToValidationProblem();

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
			.Produces(StatusCodes.Status204NoContent)
			.ProducesProblem(StatusCodes.Status400BadRequest);

		static async Task<IResult> RemoveItem(
			[FromBody] Requests.RemoveItem request,
			IValidator<Requests.RemoveItem> validator,
			ICommandHandler<RemoveShoppingItemCommand, Result> handler,
			CancellationToken cancellationToken)
		{
			var validation = await validator.ValidateAsync(request, cancellationToken);
			if (validation.HasFailed()) return validation.ToValidationProblem();

			var (itemName, quantity, reason) = request;
			var command = new RemoveShoppingItemCommand(itemName, quantity, reason);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToNoContent();
		}

		group.MapGet("/merged", GetMerged)
			.WithName("GetMergedShoppingList")
			.WithTags("Shopping")
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
			.Produces(StatusCodes.Status204NoContent)
			.ProducesValidationProblem();

		static async Task<IResult> MarkAsFrozen(
			Requests.MarkAsFrozen request,
			IValidator<Requests.MarkAsFrozen> validator,
			ICommandHandler<MarkAsFrozenCommand, Result> handler,
			CancellationToken cancellationToken)
		{
			var validation = await validator.ValidateAsync(request, cancellationToken);
			if (validation.HasFailed()) return validation.ToValidationProblem();

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
