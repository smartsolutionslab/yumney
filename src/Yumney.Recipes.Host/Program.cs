using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Api;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Recipes.Application.Queries;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Extraction;
using SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddYumneyDefaults();

builder.Services.AddDbContext<RecipesDbContext>(options =>
{
    var connection = builder.Configuration.GetConnectionString("recipesdb");
    options.UseNpgsql(connection, x => x.MigrationsHistoryTable("__RecipesMigrationsHistory"));
});

builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();

builder.Services.AddValidatorsFromAssemblyContaining<ImportRecipeRequestValidator>();
builder.Services.AddScoped<ICommandHandler<ImportRecipeCommand, Result<ExtractedRecipeDto>>, ImportRecipeCommandHandler>();
builder.Services.AddScoped<ICommandHandler<SaveRecipeCommand, Result<SavedRecipeDto>>, SaveRecipeCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetRecipesQuery, Result<PagedResult<RecipeListItemDto>>>, GetRecipesQueryHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateRecipeCommand, Result<RecipeDetailDto>>, UpdateRecipeCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteRecipeCommand, Result>, DeleteRecipeCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetRecipeByIdQuery, Result<RecipeDetailDto>>, GetRecipeByIdQueryHandler>();

builder.Services.AddRecipeExtraction(builder.Configuration);

var app = builder.Build();

app.UseYumneyDefaults();

app.MapGroup("/api/v1")
    .RequireAuthorization()
    .MapRecipesEndpoints();

app.Run();
