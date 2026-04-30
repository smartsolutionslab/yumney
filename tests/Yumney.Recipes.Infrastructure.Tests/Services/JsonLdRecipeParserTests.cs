using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Extraction.Services;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Tests.Services;

public class JsonLdRecipeParserTests
{
	[Fact]
	public void TryParse_NoJsonLdScript_ReturnsNull()
	{
		var html = "<html><body><h1>Just a blog post</h1></body></html>";

		var result = JsonLdRecipeParser.TryParse(html);

		result.Should().BeNull();
	}

	[Fact]
	public void TryParse_MinimalRecipe_ReturnsDto()
	{
		var html = Wrap("""
			{
			  "@context": "https://schema.org",
			  "@type": "Recipe",
			  "name": "Pancakes",
			  "recipeIngredient": ["200 g flour", "2 eggs", "300 ml milk"],
			  "recipeInstructions": "Mix everything. Fry in pan."
			}
			""");

		var result = JsonLdRecipeParser.TryParse(html);

		result.Should().NotBeNull();
		result!.Title.Should().Be("Pancakes");
		result.Ingredients.Should().HaveCount(3);
		result.Ingredients[0].Name.Should().Be("200 g flour");
		result.Steps.Should().ContainSingle();
	}

	[Fact]
	public void TryParse_InstructionsAsHowToSteps_FlattensToSteps()
	{
		var html = Wrap("""
			{
			  "@type": "Recipe",
			  "name": "Soup",
			  "recipeIngredient": ["tomato"],
			  "recipeInstructions": [
			    { "@type": "HowToStep", "text": "Chop tomato" },
			    { "@type": "HowToStep", "text": "Simmer" }
			  ]
			}
			""");

		var result = JsonLdRecipeParser.TryParse(html);

		result!.Steps.Should().HaveCount(2);
		result.Steps[0].Number.Should().Be(1);
		result.Steps[0].Description.Should().Be("Chop tomato");
		result.Steps[1].Number.Should().Be(2);
	}

	[Fact]
	public void TryParse_InstructionsAsHowToSections_FlattensNestedSteps()
	{
		var html = Wrap("""
			{
			  "@type": "Recipe",
			  "name": "Layered cake",
			  "recipeIngredient": ["flour"],
			  "recipeInstructions": [
			    { "@type": "HowToSection", "itemListElement": [
			      { "@type": "HowToStep", "text": "Prepare the batter" },
			      { "@type": "HowToStep", "text": "Bake" }
			    ]},
			    { "@type": "HowToSection", "itemListElement": [
			      { "@type": "HowToStep", "text": "Make frosting" }
			    ]}
			  ]
			}
			""");

		var result = JsonLdRecipeParser.TryParse(html);

		result!.Steps.Should().HaveCount(3);
		result.Steps.Select(step => step.Description).Should().Equal("Prepare the batter", "Bake", "Make frosting");
	}

	[Fact]
	public void TryParse_GraphWrapper_FindsRecipeInGraph()
	{
		var html = Wrap("""
			{
			  "@context": "https://schema.org",
			  "@graph": [
			    { "@type": "WebSite", "name": "Cooking blog" },
			    { "@type": "Recipe", "name": "Bread",
			      "recipeIngredient": ["flour", "water", "salt"],
			      "recipeInstructions": "Mix and bake." }
			  ]
			}
			""");

		var result = JsonLdRecipeParser.TryParse(html);

		result.Should().NotBeNull();
		result!.Title.Should().Be("Bread");
	}

	[Fact]
	public void TryParse_IsoDuration_ReturnsMinutes()
	{
		var html = Wrap("""
			{
			  "@type": "Recipe",
			  "name": "Roast",
			  "recipeIngredient": ["chicken"],
			  "recipeInstructions": "Roast",
			  "prepTime": "PT20M",
			  "cookTime": "PT1H30M"
			}
			""");

		var result = JsonLdRecipeParser.TryParse(html);

		result!.PrepTimeMinutes.Should().Be(20);
		result.CookTimeMinutes.Should().Be(90);
	}

	[Fact]
	public void TryParse_StringYield_ParsesLeadingDigits()
	{
		var html = Wrap("""
			{
			  "@type": "Recipe",
			  "name": "Cookies",
			  "recipeIngredient": ["flour"],
			  "recipeInstructions": "Bake",
			  "recipeYield": "12 cookies"
			}
			""");

		var result = JsonLdRecipeParser.TryParse(html);

		result!.Servings.Should().Be(12);
	}

	[Fact]
	public void TryParse_ImageAsObject_ReadsUrl()
	{
		var html = Wrap("""
			{
			  "@type": "Recipe",
			  "name": "Pie",
			  "recipeIngredient": ["apple"],
			  "recipeInstructions": "Bake",
			  "image": { "@type": "ImageObject", "url": "https://example.com/pie.jpg" }
			}
			""");

		var result = JsonLdRecipeParser.TryParse(html);

		result!.ImageUrl.Should().Be("https://example.com/pie.jpg");
	}

	[Fact]
	public void TryParse_TypeArray_RecognizesRecipe()
	{
		var html = Wrap("""
			{
			  "@type": ["Recipe", "Thing"],
			  "name": "Salad",
			  "recipeIngredient": ["lettuce"],
			  "recipeInstructions": "Toss"
			}
			""");

		var result = JsonLdRecipeParser.TryParse(html);

		result.Should().NotBeNull();
		result!.Title.Should().Be("Salad");
	}

	[Fact]
	public void TryParse_MissingIngredients_ReturnsNullSoLlmFallsBack()
	{
		var html = Wrap("""
			{
			  "@type": "Recipe",
			  "name": "Skeletal",
			  "recipeInstructions": "Do nothing"
			}
			""");

		var result = JsonLdRecipeParser.TryParse(html);

		result.Should().BeNull();
	}

	[Fact]
	public void TryParse_MissingInstructions_ReturnsNullSoLlmFallsBack()
	{
		var html = Wrap("""
			{
			  "@type": "Recipe",
			  "name": "No steps",
			  "recipeIngredient": ["water"]
			}
			""");

		var result = JsonLdRecipeParser.TryParse(html);

		result.Should().BeNull();
	}

	[Fact]
	public void TryParse_MalformedJson_SkipsToNextBlob()
	{
		var html = """
			<script type="application/ld+json">{ this is not valid json }</script>
			<script type="application/ld+json">
			{ "@type": "Recipe", "name": "Survivor",
			  "recipeIngredient": ["a"], "recipeInstructions": "do" }
			</script>
			""";

		var result = JsonLdRecipeParser.TryParse(html);

		result!.Title.Should().Be("Survivor");
	}

	[Fact]
	public void TryParse_NonRecipeJsonLd_ReturnsNull()
	{
		var html = Wrap("""
			{ "@type": "Article", "headline": "Some news" }
			""");

		var result = JsonLdRecipeParser.TryParse(html);

		result.Should().BeNull();
	}

	private static string Wrap(string json) =>
		$"<!doctype html><html><head><script type=\"application/ld+json\">{json}</script></head><body></body></html>";
}
