using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Yumney.Recipes.Application.Commands;
using Yumney.Recipes.Domain.Recipe;

namespace Yumney.Recipes.Application.Tests.Commands;

public class ImportRecipeCommandHandlerTests
{
    private readonly ILogger<ImportRecipeCommandHandler> logger =
        Substitute.For<ILogger<ImportRecipeCommandHandler>>();

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccess()
    {
        var sut = new ImportRecipeCommandHandler(logger);
        var command = new ImportRecipeCommand(
            new RecipeUrl("https://example.com/recipe"));

        var result = await sut.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsMessageDto()
    {
        var sut = new ImportRecipeCommandHandler(logger);
        var command = new ImportRecipeCommand(
            new RecipeUrl("https://example.com/recipe"));

        var result = await sut.HandleAsync(command);

        result.Value.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_DoesNotReturnFailure()
    {
        var sut = new ImportRecipeCommandHandler(logger);
        var command = new ImportRecipeCommand(
            new RecipeUrl("https://example.com/recipe/pasta"));

        var result = await sut.HandleAsync(command);

        result.IsFailure.Should().BeFalse();
    }
}
