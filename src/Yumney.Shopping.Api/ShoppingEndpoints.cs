using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Api.Requests;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

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
            .Produces<IReadOnlyList<ShoppingListSummaryDto>>();

        group.MapGet("/{identifier:guid}", GetByIdAsync)
            .WithName("GetShoppingListById")
            .WithTags("Shopping")
            .Produces<ShoppingListDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateShoppingListRequest request,
        IValidator<CreateShoppingListRequest> validator,
        ICommandHandler<CreateShoppingListCommand, Result<ShoppingListDetailDto>> handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var command = new CreateShoppingListCommand(
            new ShoppingListTitle(request.Title),
            request.Items.Select(i => new CreateShoppingListItemCommand(
                new ItemName(i.Name),
                Amount.FromNullable(i.Amount),
                Unit.FromNullable(i.Unit))).ToList(),
            RecipeReference.FromNullable(request.RecipeReference));
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Created($"/api/v1/shopping-lists/{result.Value.Identifier}", result.Value);
    }

    private static async Task<IResult> GetAllAsync(
        IQueryHandler<GetShoppingListsQuery, Result<IReadOnlyList<ShoppingListSummaryDto>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetShoppingListsQuery();
        var result = await handler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid identifier,
        IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListDetailDto>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetShoppingListByIdQuery(new ShoppingListIdentifier(identifier));
        var result = await handler.HandleAsync(query, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Problem(result.Error!.Message, statusCode: result.Error.HttpStatusCode);
        }

        return Results.Ok(result.Value);
    }
}
