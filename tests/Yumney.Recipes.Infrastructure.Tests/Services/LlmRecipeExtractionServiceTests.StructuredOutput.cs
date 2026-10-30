using FluentAssertions;
using Microsoft.Extensions.AI;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.TestBuilders.Llm;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

#pragma warning disable SA1601
public partial class LlmRecipeExtractionServiceTests
#pragma warning restore SA1601
{
	[Fact]
	public async Task ExtractAsync_RequestsJsonSchemaOutput()
	{
		var fake = new ScriptedChatClient(validRecipeJson);

		await CreateService(fake).ExtractAsync(new ScrapedContent("text", RecipeUrl.From("https://example.com/r")));

		fake.CapturedOptions!.ResponseFormat.Should().BeOfType<ChatResponseFormatJson>()
			.Which.Schema.Should().NotBeNull();
	}

	[Fact]
	public async Task ExtractAsync_ReplyWithoutTitle_ReturnsExtractionFailed()
	{
		var service = CreateService("""{ "ingredients": [], "steps": [] }""");

		var result = await service.ExtractAsync(new ScrapedContent("text", RecipeUrl.From("https://example.com/r")));

		result.Error.Should().Be(ImportRecipeErrors.ExtractionFailed);
	}

	[Fact]
	public async Task ExtractFromPhotosAsync_SendsEveryPhotoAsImage()
	{
		var fake = new ScriptedChatClient(validRecipeJson);
		PhotoData[] photos = [new([1, 2, 3], "image/jpeg", "page1.jpg"), new([4, 5, 6], "image/png", "page2.png")];

		await CreateService(fake).ExtractFromPhotosAsync(photos);

		fake.CapturedMessages![^1].Contents.OfType<DataContent>().Select(image => image.MediaType)
			.Should().Equal("image/jpeg", "image/png");
	}

	[Fact]
	public async Task StreamExtractAsync_StreamsTheModelReplyInJsonMode()
	{
		var fake = new ScriptedChatClient(validRecipeJson);
		var content = new ScrapedContent("text", RecipeUrl.From("https://example.com/r"));

		var chunks = await CreateService(fake).StreamExtractAsync(content).ToListAsync();

		string.Concat(chunks).Should().Be(validRecipeJson);
		fake.CapturedOptions!.ResponseFormat.Should().Be(ChatResponseFormat.Json);
	}
}
