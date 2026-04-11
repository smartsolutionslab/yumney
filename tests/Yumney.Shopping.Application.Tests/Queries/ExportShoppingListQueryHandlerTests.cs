using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Application.DTOs;
using SmartSolutionsLab.Yumney.Shopping.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries;
using SmartSolutionsLab.Yumney.Shopping.Application.Queries.Handlers;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shopping.Application.Tests.Queries;

public class ExportShoppingListQueryHandlerTests
{
    private readonly IShoppingListReadModelRepository readModel = Substitute.For<IShoppingListReadModelRepository>();
    private readonly ICurrentUser currentUser = Substitute.For<ICurrentUser>();
    private readonly ExportShoppingListQueryHandler handler;

    public ExportShoppingListQueryHandlerTests()
    {
        currentUser.UserId.Returns("user-123");
        handler = new ExportShoppingListQueryHandler(readModel, currentUser);
    }

    [Fact]
    public async Task HandleAsync_NoItems_ReturnsEmptyString()
    {
        readModel.GetByOwnerAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(new MergedShoppingListDto([]));

        var result = await handler.HandleAsync(new ExportShoppingListQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_AllBought_ReturnsEmptyString()
    {
        var items = new List<MergedShoppingItemDto>
        {
            new("Milk", 2, 2, "L", "dairy", true, []),
        };
        readModel.GetByOwnerAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(new MergedShoppingListDto(items));

        var result = await handler.HandleAsync(new ExportShoppingListQuery());

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_OpenItems_FormatsGroupedByCategory()
    {
        var items = new List<MergedShoppingItemDto>
        {
            new("Chicken", 500, 500, "g", "meat-fish", false, []),
            new("Milk", 2, 2, "L", "dairy", false, []),
            new("Onion", 3, 3, "pc", "produce", false, []),
        };
        readModel.GetByOwnerAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(new MergedShoppingListDto(items));

        var result = await handler.HandleAsync(new ExportShoppingListQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Produce");
        result.Value.Should().Contain("Dairy");
        result.Value.Should().Contain("Meat & Fish");
        result.Value.Should().Contain("Onion");
        result.Value.Should().Contain("Milk");
        result.Value.Should().Contain("Chicken");
    }

    [Fact]
    public async Task HandleAsync_ExcludesBoughtItems()
    {
        var items = new List<MergedShoppingItemDto>
        {
            new("Milk", 2, 2, "L", "dairy", true, []),
            new("Eggs", 6, 6, "pc", "dairy", false, []),
        };
        readModel.GetByOwnerAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(new MergedShoppingListDto(items));

        var result = await handler.HandleAsync(new ExportShoppingListQuery());

        result.Value.Should().Contain("Eggs");
        result.Value.Should().NotContain("Milk");
    }

    [Fact]
    public async Task HandleAsync_RoundsQuantities()
    {
        var items = new List<MergedShoppingItemDto>
        {
            new("Milk", 5.3m, 6, "L", "dairy", false, []),
        };
        readModel.GetByOwnerAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(new MergedShoppingListDto(items));

        var result = await handler.HandleAsync(new ExportShoppingListQuery());

        result.Value.Should().Contain("6 L Milk");
    }

    [Fact]
    public async Task HandleAsync_OrderedByCategoryDisplayOrder()
    {
        var items = new List<MergedShoppingItemDto>
        {
            new("Soap", 1, 1, "pc", "household", false, []),
            new("Onion", 1, 1, "pc", "produce", false, []),
        };
        readModel.GetByOwnerAsync("user-123", Arg.Any<CancellationToken>())
            .Returns(new MergedShoppingListDto(items));

        var result = await handler.HandleAsync(new ExportShoppingListQuery());

        var produceIndex = result.Value.IndexOf("Produce", StringComparison.Ordinal);
        var householdIndex = result.Value.IndexOf("Household", StringComparison.Ordinal);
        produceIndex.Should().BeLessThan(householdIndex);
    }
}
