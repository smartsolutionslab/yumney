using FluentAssertions;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using Xunit;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Tests.DTOs;

public class IntentToActionMapperTests
{
	[Theory]
	[InlineData("shopping-list", "/shopping")]
	[InlineData("meal-planner", "/meal-planner")]
	[InlineData("recipes", "/recipes")]
	[InlineData("settings", "/account")]
	public void Map_NavigateIntentWithKnownTarget_ReturnsNavigateAction(string target, string expectedRoute)
	{
		var intent = new ParsedIntentDto(
			"navigate",
			new Dictionary<string, string> { ["target"] = target },
			Clarification: null);

		var actions = IntentToActionMapper.Map(intent);

		actions.Should().ContainSingle();
		actions[0].Type.Should().Be(ChatActionType.Navigate);
		actions[0].Route.Should().Be(expectedRoute);
		actions[0].RecipeIdentifier.Should().BeNull();
	}

	[Fact]
	public void Map_NavigateIntentWithUnknownTarget_ReturnsEmpty()
	{
		var intent = new ParsedIntentDto(
			"navigate",
			new Dictionary<string, string> { ["target"] = "moon-base" },
			Clarification: null);

		var actions = IntentToActionMapper.Map(intent);

		actions.Should().BeEmpty();
	}

	[Fact]
	public void Map_NavigateIntentWithoutTargetEntity_ReturnsEmpty()
	{
		var intent = new ParsedIntentDto("navigate", new Dictionary<string, string>(), Clarification: null);

		var actions = IntentToActionMapper.Map(intent);

		actions.Should().BeEmpty();
	}

	[Theory]
	[InlineData("general_chat")]
	[InlineData("search_recipe")]
	[InlineData("plan_meal")]
	[InlineData("what_can_i_cook")]
	[InlineData("add_to_list")]
	public void Map_NonNavigateIntent_ReturnsEmpty(string intentName)
	{
		var intent = new ParsedIntentDto(intentName, new Dictionary<string, string>(), Clarification: null);

		var actions = IntentToActionMapper.Map(intent);

		actions.Should().BeEmpty();
	}
}
