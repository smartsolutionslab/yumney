using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.Commands;

public class ParseIntentCommandHandlerTests
{
    private readonly IIntentParserService intentParser = Substitute.For<IIntentParserService>();
    private readonly ILogger<ParseIntentCommandHandler> logger = Substitute.For<ILogger<ParseIntentCommandHandler>>();
    private readonly ParseIntentCommandHandler handler;

    public ParseIntentCommandHandlerTests()
    {
        handler = new ParseIntentCommandHandler(intentParser, logger);
    }

    [Fact]
    public async Task HandleAsync_ValidMessage_ReturnsSuccess()
    {
        var expected = new ParsedIntentDto("add_to_list", new Dictionary<string, string> { ["item"] = "milk" }, null);
        intentParser.ParseAsync("Add milk", null, Arg.Any<CancellationToken>())
            .Returns(Result<ParsedIntentDto>.Success(expected));

        var command = new ParseIntentCommand("Add milk", null);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("add_to_list");
        result.Value.Entities["item"].Should().Be("milk");
    }

    [Fact]
    public async Task HandleAsync_WithPageContext_PassesContextToService()
    {
        var expected = new ParsedIntentDto("add_to_list", new Dictionary<string, string> { ["item"] = "milk" }, null);
        intentParser.ParseAsync("Add milk", "shopping-list", Arg.Any<CancellationToken>())
            .Returns(Result<ParsedIntentDto>.Success(expected));

        var command = new ParseIntentCommand("Add milk", "shopping-list");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        await intentParser.Received(1).ParseAsync("Add milk", "shopping-list", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ServiceReturnsFailure_ReturnsFailure()
    {
        intentParser.ParseAsync("broken", null, Arg.Any<CancellationToken>())
            .Returns(new ApiError("intent.parse.failed", "Failed", 502));

        var command = new ParseIntentCommand("broken", null);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("intent.parse.failed");
    }

    [Fact]
    public async Task HandleAsync_AmbiguousInput_ReturnsClarification()
    {
        var expected = new ParsedIntentDto(
            "general_chat",
            new Dictionary<string, string>(),
            "Search for pasta recipes or add pasta to your shopping list?");
        intentParser.ParseAsync("pasta", null, Arg.Any<CancellationToken>())
            .Returns(Result<ParsedIntentDto>.Success(expected));

        var command = new ParseIntentCommand("pasta", null);

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Intent.Should().Be("general_chat");
        result.Value.Clarification.Should().NotBeNull();
    }
}
