using FluentValidation;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shopping.Api.Requests;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using ShoppingListItem = SmartSolutionsLab.Yumney.Shopping.Application.Commands.ShoppingListItem;

namespace SmartSolutionsLab.Yumney.Shopping.Api;

public static class ShoppingEndpoints
{
    public static IEndpointRouteBuilder MapShoppingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/shopping-lists");

        group.MapPost("/", CreateAsync)
            .WithName("CreateShoppingList")
            .WithTags("Shopping")
            .Produces<ShoppingListDetailDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/", GetAllAsync)
            .WithName("GetShoppingLists")
            .WithTags("Shopping")
            .Produces<PagedResult<ShoppingListSummaryDto>>();

        group.MapGet("/{identifier:guid}", GetByIdAsync)
            .WithName("GetShoppingListById")
            .WithTags("Shopping")
            .Produces<ShoppingListDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{identifier:guid}/items/{itemId:guid}/check", CheckOffItemAsync)
            .WithName("CheckOffItem")
            .WithTags("Shopping")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{identifier:guid}/check-all", CheckOffAllItemsAsync)
            .WithName("CheckOffAllItems")
            .WithTags("Shopping")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/items", AddManualItemAsync)
            .WithName("AddManualItem")
            .WithTags("Shopping")
            .Produces<AddedItemDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapDelete("/items", RemoveItemAsync)
            .WithName("RemoveShoppingItem")
            .WithTags("Shopping")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/merged", GetMergedAsync)
            .WithName("GetMergedShoppingList")
            .WithTags("Shopping")
            .Produces<MergedShoppingListDto>();

        group.MapPost("/shopping-mode/start", StartShoppingModeAsync)
            .WithName("StartShoppingMode")
            .WithTags("Shopping")
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/shopping-mode/end", EndShoppingModeAsync)
            .WithName("EndShoppingMode")
            .WithTags("Shopping")
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/export", ExportAsync)
            .WithName("ExportShoppingList")
            .WithTags("Shopping")
            .Produces<string>(contentType: "text/plain");

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateShoppingListRequest request,
        IValidator<CreateShoppingListRequest> validator,
        ICommandHandler<CreateShoppingListCommand, Result<ShoppingListDetailDto>> handler,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.HasFailed()) return validation.ToValidationProblem();

        var (title, items, recipeReference) = request;

        var command = new CreateShoppingListCommand(
            ShoppingListTitle.From(title),
            request.Items.Select(i => new ShoppingListItem(
                ItemName.From(i.Name),
                Quantity.FromNullable(
                    Amount.FromNullable(i.Amount),
                    Unit.FromNullable(i.Unit)))).ToList(),
            RecipeReference.FromNullable(recipeReference));

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToCreated($"/api/v1/shopping-lists/{result.Value?.Identifier}");
    }

    private static async Task<IResult> GetAllAsync(
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

    private static async Task<IResult> GetByIdAsync(
        Guid identifier,
        IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListDetailDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetShoppingListByIdQuery(ShoppingListIdentifier.From(identifier));

        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> CheckOffItemAsync(
        Guid identifier,
        Guid itemId,
        CheckOffItemRequest request,
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

    private static async Task<IResult> CheckOffAllItemsAsync(
        Guid identifier,
        CheckOffItemRequest request,
        ICommandHandler<CheckOffAllItemsCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var command = new CheckOffAllItemsCommand(
            ShoppingListIdentifier.From(identifier),
            request.IsChecked);

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToNoContent();
    }

    private static async Task<IResult> AddManualItemAsync(
        AddManualItemRequest request,
        IValidator<AddManualItemRequest> validator,
        ICommandHandler<AddManualItemCommand, Result<AddedItemDto>> handler,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.HasFailed()) return validation.ToValidationProblem();

        var command = new AddManualItemCommand(ItemName.From(request.Name.Trim()), request.Quantity, request.Unit);

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/shopping-lists/items/{result.Value.LedgerIdentifier}", result.Value)
            : result.ToOk();
    }

    private static async Task<IResult> GetMergedAsync(
        IQueryHandler<GetMergedShoppingListQuery, Result<MergedShoppingListDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetMergedShoppingListQuery();
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToOk();
    }

    private static async Task<IResult> StartShoppingModeAsync(
        ICommandHandler<StartShoppingModeCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new StartShoppingModeCommand(), cancellationToken);
        return result.ToNoContent();
    }

    private static async Task<IResult> EndShoppingModeAsync(
        EndShoppingModeRequest request,
        ICommandHandler<EndShoppingModeCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new EndShoppingModeCommand(request.AcceptPendingChanges), cancellationToken);
        return result.ToNoContent();
    }

    private static async Task<IResult> ExportAsync(
        IQueryHandler<ExportShoppingListQuery, Result<string>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ExportShoppingListQuery(), cancellationToken);
        if (result.IsFailure)
            return result.ToOk();

        return Results.Text(result.Value, "text/plain");
    }

    private static async Task<IResult> RemoveItemAsync(
        RemoveItemRequest request,
        IValidator<RemoveItemRequest> validator,
        ICommandHandler<RemoveShoppingItemCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.HasFailed()) return validation.ToValidationProblem();

        var command = new RemoveShoppingItemCommand(ItemName.From(request.Name.Trim()), request.Quantity, request.Unit, request.Reason);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToNoContent();
    }
}
