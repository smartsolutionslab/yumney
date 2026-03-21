using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands.Handler;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Application.Requests;
using GetShoppingListByIdQueryHandler = SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers.GetShoppingListByIdQueryHandler;
using GetShoppingListsQueryHandler = SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers.GetShoppingListsQueryHandler;

namespace SmartSolutionsLab.Yumney.Shopping.Application;

public static class ShoppingApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddShoppingApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateShoppingListRequestValidator>();

        services.AddScoped<ICommandHandler<CreateShoppingListCommand, Result<ShoppingListDetailDto>>, CreateShoppingListCommandHandler>();

        services.AddScoped<IQueryHandler<GetShoppingListsQuery, Result<IReadOnlyList<ShoppingListSummaryDto>>>, GetShoppingListsQueryHandler>();
        services.AddScoped<IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListDetailDto>>, GetShoppingListByIdQueryHandler>();

        return services;
    }
}
