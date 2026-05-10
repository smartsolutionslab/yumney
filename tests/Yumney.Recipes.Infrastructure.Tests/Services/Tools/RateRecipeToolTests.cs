using FluentAssertions;
using NSubstitute;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services.Tools;

public class RateRecipeToolTests
{
	private readonly ICommandHandler<RateRecipeCommand, Result> handler =
		Substitute.For<ICommandHandler<RateRecipeCommand, Result>>();

	[Fact]
	public async Task RateAsync_HappyPath_DispatchesAndReturnsConfirmation()
	{
		var id = Guid.NewGuid();
		RateRecipeCommand? captured = null;
		handler.HandleAsync(Arg.Any<RateRecipeCommand>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				captured = callInfo.ArgAt<RateRecipeCommand>(0);
				return Result.Success();
			});

		var tool = new RateRecipeTool(handler);
		var reply = await tool.RateAsync(id.ToString(), 5);

		reply.Should().Contain("5-star");
		captured.Should().NotBeNull();
		captured!.Identifier.Value.Should().Be(id);
		captured.Rating.Value.Should().Be(5);
	}

	[Fact]
	public async Task RateAsync_InvalidGuid_ReturnsErrorWithoutCallingHandler()
	{
		var tool = new RateRecipeTool(handler);
		var reply = await tool.RateAsync("not-a-guid", 4);

		reply.Should().Contain("Invalid recipe identifier");
		await handler.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(6)]
	[InlineData(-1)]
	[InlineData(99)]
	public async Task RateAsync_OutOfRangeRating_ReturnsErrorWithoutCallingHandler(int rating)
	{
		var tool = new RateRecipeTool(handler);
		var reply = await tool.RateAsync(Guid.NewGuid().ToString(), rating);

		reply.Should().Contain("between 1 and 5");
		await handler.DidNotReceiveWithAnyArgs().HandleAsync(default!, default);
	}

	[Fact]
	public async Task RateAsync_HandlerFailure_ReturnsErrorMessage()
	{
		handler.HandleAsync(Arg.Any<RateRecipeCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Failure(new ApiError("ACCESS_DENIED", "Access denied", 403)));

		var tool = new RateRecipeTool(handler);
		var reply = await tool.RateAsync(Guid.NewGuid().ToString(), 4);

		reply.Should().Contain("Couldn't save");
	}
}
