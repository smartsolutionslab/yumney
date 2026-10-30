using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using SmartSolutionsLab.Yumney.TestBuilders.Llm;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class LlmIngredientRecognitionServiceTests
{
	private static readonly PhotoData Photo = new([1, 2, 3], "image/jpeg", "fridge.jpg");

	[Fact]
	public async Task RecognizeAsync_ValidReply_ReturnsIngredients()
	{
		var chatClient = new ScriptedChatClient("""{"ingredients":[{"name":"Tomato","confidence":0.9,"category":"produce"}]}""");

		var result = await Recognizer(chatClient).RecognizeAsync(Photo);

		result.Value.Ingredients.Should().ContainSingle().Which.Name.Should().Be("Tomato");
	}

	[Fact]
	public async Task RecognizeAsync_SendsPhotoAsImage()
	{
		var chatClient = new ScriptedChatClient("""{"ingredients":[]}""");

		await Recognizer(chatClient).RecognizeAsync(Photo);

		chatClient.CapturedMessages![^1].Contents.OfType<DataContent>().Should().ContainSingle()
			.Which.MediaType.Should().Be("image/jpeg");
	}

	[Fact]
	public async Task RecognizeAsync_LlmFails_ReturnsExtractionFailed()
	{
		var result = await Recognizer(new ScriptedChatClient(new HttpRequestException("down"))).RecognizeAsync(Photo);

		result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
	}

	private static LlmIngredientRecognitionService Recognizer(ScriptedChatClient chatClient) =>
		new(chatClient, NullLogger<LlmIngredientRecognitionService>.Instance);
}
