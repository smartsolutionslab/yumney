using SmartSolutionsLab.Yumney.Recipes.Api.Requests;
using SmartSolutionsLab.Yumney.Recipes.Application.Commands;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Chat;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Api;

internal static class RequestMappingExtensions
{
	extension(SaveRecipeIngredientRequest request)
	{
		public SaveRecipeIngredientItem ToCommandItem()
		{
			var (name, amount, unit) = request;

			return new SaveRecipeIngredientItem(
				IngredientName.From(name),
				Quantity.FromNullable(
					Amount.FromNullable(amount),
					Unit.FromNullable(unit)));
		}
	}

	extension(IEnumerable<SaveRecipeIngredientRequest> items)
	{
		public IEnumerable<SaveRecipeIngredientItem> MapToRecipeIngredientItems()
		{
			foreach (var item in items)
			{
				yield return item.ToCommandItem();
			}
		}
	}

	extension(SaveRecipeStepRequest request)
	{
		public SaveRecipeStepItem ToCommandItem()
		{
			var (number, description) = request;

			return new SaveRecipeStepItem(
				StepNumber.From(number),
				StepDescription.From(description));
		}
	}

	extension(IEnumerable<SaveRecipeStepRequest> items)
	{
		public IEnumerable<SaveRecipeStepItem> MapToRecipeStepItems()
		{
			foreach (var item in items)
			{
				yield return item.ToCommandItem();
			}
		}
	}

	extension(IEnumerable<ChatMessageDto> history)
	{
		public IEnumerable<ChatHistoryEntry> MapToChatHistoryEntries()
		{
			return history.Select(h => new ChatHistoryEntry(
				ChatRole.From(h.Role),
				ChatMessageContent.From(h.Content)));
		}
	}
}
