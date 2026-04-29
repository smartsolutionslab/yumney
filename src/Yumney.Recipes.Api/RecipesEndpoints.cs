using FluentValidation;
using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

#pragma warning disable SA1601 // Partial endpoints class is split for file-size reasons; documentation lives on individual methods.
public static partial class RecipesEndpoints
#pragma warning restore SA1601
{
	public static IEndpointRouteBuilder MapRecipesEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/recipes");

		MapCrudEndpoints(group);
		MapImportEndpoints(group);
		MapChatEndpoints(group);

		return app;
	}

	private static void MapCrudEndpoints(RouteGroupBuilder group)
	{
		group.MapGet("/", GetAll)
			.WithName("GetRecipes")
			.WithTags("Recipes")
			.Produces<PagedResult<RecipeListItemDto>>();

		static async Task<IResult> GetAll(
			IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>> handler,
			int page = PagingOptions.DefaultPage,
			int pageSize = PagingOptions.DefaultPageSize,
			string sortBy = "Date",
			SortDirection sortDirection = SortDirection.Descending,
			string? search = null,
			string? tags = null,
			string? difficulty = null,
			int? maxPrepTime = null,
			int? maxCookTime = null,
			bool? favorites = null,
			CancellationToken cancellationToken = default)
		{
			var query = new GetRecipesQuery(
				PagingOptions.From(page, pageSize),
				SortingOptions<RecipeSortField>.Parse(sortBy, sortDirection, RecipeSortField.Date),
				SearchTerm.FromNullable(search),
				RecipeFilterParser.Build(tags, difficulty, maxPrepTime, maxCookTime, favorites));

			var result = await handler.HandleAsync(query, cancellationToken);
			return result.ToOk();
		}

		group.MapGet("/{identifier:guid}", GetById)
			.WithName("GetRecipeById")
			.WithTags("Recipes")
			.Produces<RecipeDetailDto>()
			.ProducesProblem(StatusCodes.Status404NotFound);

		static async Task<IResult> GetById(
			Guid identifier,
			IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>> handler,
			CancellationToken cancellationToken)
		{
			var query = new GetRecipeByIdQuery(RecipeIdentifier.From(identifier));
			var result = await handler.HandleAsync(query, cancellationToken);
			return result.ToOk();
		}

		group.MapPost("/", Save)
			.WithName("SaveRecipe")
			.WithTags("Recipes")
			.Produces<SavedRecipeDto>(StatusCodes.Status201Created)
			.ProducesValidationProblem()
			.ProducesProblem(StatusCodes.Status409Conflict);

		static async Task<IResult> Save(
			SaveRecipeRequest request,
			IValidator<SaveRecipeRequest> validator,
			ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>> handler,
			CancellationToken cancellationToken)
		{
			var validation = await validator.ValidateAsync(request, cancellationToken);
			if (validation.HasFailed()) return validation.ToValidationProblem();

			var (title, ingredients, steps, description, servings, timing, difficulty, imageUrl, language, sourceUrl, tags) = request;

			var command = new SaveRecipeCommand(
				title,
				ingredients,
				steps,
				description,
				servings,
				timing,
				difficulty,
				imageUrl,
				language,
				sourceUrl,
				tags);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToCreated($"/api/v1/recipes/{result.Value.Identifier}");
		}

		group.MapPut("/{identifier:guid}", Update)
			.WithName("UpdateRecipe")
			.WithTags("Recipes")
			.Produces<RecipeDetailDto>()
			.ProducesValidationProblem()
			.ProducesProblem(StatusCodes.Status404NotFound);

		static async Task<IResult> Update(
			Guid identifier,
			UpdateRecipeRequest request,
			IValidator<UpdateRecipeRequest> validator,
			ICommandHandler<UpdateRecipeCommand, Result<RecipeDetailDto>> handler,
			CancellationToken cancellationToken)
		{
			var validation = await validator.ValidateAsync(request, cancellationToken);
			if (validation.HasFailed()) return validation.ToValidationProblem();

			var (title, ingredients, steps, description, servings, timing, difficulty, imageUrl, tags) = request;
			var command = new UpdateRecipeCommand(RecipeIdentifier.From(identifier), title, ingredients, steps, description, servings, timing, difficulty, imageUrl, tags);

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		group.MapDelete("/{identifier:guid}", Delete)
			.WithName("DeleteRecipe")
			.WithTags("Recipes")
			.Produces(StatusCodes.Status204NoContent)
			.ProducesProblem(StatusCodes.Status404NotFound);

		static async Task<IResult> Delete(
			Guid identifier,
			ICommandHandler<DeleteRecipeCommand, Result> handler,
			CancellationToken cancellationToken)
		{
			var command = new DeleteRecipeCommand(RecipeIdentifier.From(identifier));

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToNoContent();
		}

		group.MapPost("/{identifier:guid}/favorite", ToggleFavorite)
			.WithName("ToggleRecipeFavorite")
			.WithTags("Recipes")
			.Produces<FavoriteStateDto>()
			.ProducesProblem(StatusCodes.Status404NotFound);

		static async Task<IResult> ToggleFavorite(
			Guid identifier,
			ICommandHandler<ToggleFavoriteCommand, Result<FavoriteStateDto>> handler,
			CancellationToken cancellationToken)
		{
			var command = new ToggleFavoriteCommand(RecipeIdentifier.From(identifier));

			var result = await handler.HandleAsync(command, cancellationToken);
			return result.ToOk();
		}

		group.MapGet("/what-can-i-cook", WhatCanICook)
			.WithName("WhatCanICook")
			.WithTags("Recipes")
			.Produces<IReadOnlyList<CookableRecipeDto>>();

		static async Task<IResult> WhatCanICook(
			IQueryHandler<GetCookableRecipesQuery, Result<IReadOnlyList<CookableRecipeDto>>> handler,
			CancellationToken cancellationToken,
			bool fullMatchOnly = false)
		{
			var result = await handler.HandleAsync(new GetCookableRecipesQuery(fullMatchOnly), cancellationToken);
			return result.ToOk();
		}
	}
}
