using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Shopping.Api;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddYumneyDefaults();

builder.Services.AddDbContext<ShoppingDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("shoppingdb"),
        x => x.MigrationsHistoryTable("__ShoppingMigrationsHistory")));

builder.Services.AddScoped<IShoppingListRepository, ShoppingListRepository>();

builder.Services.AddValidatorsFromAssemblyContaining<CreateShoppingListRequestValidator>();
builder.Services.AddScoped<ICommandHandler<CreateShoppingListCommand, Result<ShoppingListDetailDto>>, CreateShoppingListCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetShoppingListsQuery, Result<IReadOnlyList<ShoppingListSummaryDto>>>, GetShoppingListsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListDetailDto>>, GetShoppingListByIdQueryHandler>();

var app = builder.Build();

app.UseYumneyDefaults();

app.MapGroup("/api/v1")
    .RequireAuthorization()
    .MapShoppingEndpoints();

app.Run();
