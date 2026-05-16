using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Capabilities;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Paging;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shared.Web.Capabilities;

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
		MapHistoryEndpoints(group);
		MapRatingAndNotesEndpoints(group);

		return app;
	}

	private static void MapCrudEndpoints(RouteGroupBuilder group)
	{
		group.MapGet("/", GetAll)
			.WithName("GetRecipes")
			.WithTags("Recipes")
			.WithCapability(
				name: "search_recipes",
				description: "Search the user's recipe collection by free text query and optional filters (tags, difficulty, max prep/cook time, favorites). Returns paged recipe summaries. Use for 'find pasta recipes' / 'easy 30-minute dinners' / 'zeig mir vegetarische Rezepte'.",
				surfaces: CapabilitySurface.All)
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
			.WithCapability(
				name: "get_recipe",
				description: "Fetch full details (ingredients, steps, timings, servings) of one recipe by its identifier. Use for 'show me the carbonara recipe' / 'what's in the risotto?' (call search_recipes first to get the identifier).",
				surfaces: CapabilitySurface.All)
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
			.WithValidation<Requests.SaveRecipe>()
			.Produces<SavedRecipeDto>(StatusCodes.Status201Created)
			.ProducesValidationProblem()
			.ProducesProblem(StatusCodes.Status409Conflict);

		static async Task<IResult> Save(
			Requests.SaveRecipe request,
			ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>> handler,
			CancellationToken cancellationToken)
		{
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
			.WithValidation<Requests.UpdateRecipe>()
			.Produces<RecipeDetailDto>()
			.ProducesValidationProblem()
			.ProducesProblem(StatusCodes.Status404NotFound);

		static async Task<IResult> Update(
			Guid identifier,
			Requests.UpdateRecipe request,
			ICommandHandler<UpdateRecipeCommand, Result<RecipeDetailDto>> handler,
			CancellationToken cancellationToken)
		{
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
			.WithCapability(
				name: "get_cookable_recipes",
				description: "Find recipes the user can cook now or with at most a couple of missing items, ranked by ingredient freshness in their pantry. Use for 'what can I cook?' / 'was kann ich kochen?'.",
				surfaces: CapabilitySurface.All)
			.Produces<PagedResult<CookableRecipeDto>>();

		static async Task<IResult> WhatCanICook(
			IQueryHandler<GetCookableRecipesQuery, Result<PagedResult<CookableRecipeDto>>> handler,
			CancellationToken cancellationToken,
			int page = PagingOptions.DefaultPage,
			int pageSize = PagingOptions.DefaultPageSize,
			bool fullMatchOnly = false)
		{
			var query = new GetCookableRecipesQuery(PagingOptions.From(page, pageSize), fullMatchOnly);
			var result = await handler.HandleAsync(query, cancellationToken);
			return result.ToOk();
		}

		group.MapGet("/suggestions", GetSuggestions)
			.WithName("GetRecipeSuggestions")
			.WithTags("Recipes")
			.Produces<IReadOnlyList<ExtractedRecipeDto>>()
			.ProducesProblem(StatusCodes.Status422UnprocessableEntity)
			.ProducesProblem(StatusCodes.Status502BadGateway);

		static async Task<IResult> GetSuggestions(
			IQueryHandler<GetRecipeSuggestionsQuery, Result<IReadOnlyList<ExtractedRecipeDto>>> handler,
			CancellationToken cancellationToken,
			int count = 4)
		{
			var result = await handler.HandleAsync(new GetRecipeSuggestionsQuery(count), cancellationToken);
			return result.ToOk();
		}
	}
}
