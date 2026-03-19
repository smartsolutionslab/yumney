using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shopping.Application.Commands;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;

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
