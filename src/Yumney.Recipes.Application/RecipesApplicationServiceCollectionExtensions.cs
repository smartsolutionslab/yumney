using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries.Handlers;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application;

public static class RecipesApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddRecipesApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ImportRecipeRequestValidator>();

        services.AddScoped<ICommandHandler<ImportRecipeCommand, Result<ExtractedRecipeDto>>, ImportRecipeCommandHandler>();
        services.AddScoped<ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>>, SaveRecipeCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateRecipeCommand, Result<RecipeDetailDto>>, UpdateRecipeCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteRecipeCommand, Result>, DeleteRecipeCommandHandler>();

        services.AddScoped<IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>>, GetRecipesQueryHandler>();
        services.AddScoped<IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>>, GetRecipeByIdQueryHandler>();

        return services;
    }
}
