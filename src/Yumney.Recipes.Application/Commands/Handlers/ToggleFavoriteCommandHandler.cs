using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class ToggleFavoriteCommandHandler(
	IRecipeRepository recipes,
	IRecipeFavoriteRepository favorites,
	ICurrentUser currentUser)
	: ICommandHandler<ToggleFavoriteCommand, Result<FavoriteStateDto>>
{
	public async Task<Result<FavoriteStateDto>> HandleAsync(ToggleFavoriteCommand command, CancellationToken cancellationToken = default)
	{
		var identifier = command.Identifier;
		var owner = currentUser.AsOwner();

		var recipe = await recipes.GetByIdAsync(identifier, cancellationToken);

		if (recipe.Owner != owner)
		{
			return ToggleFavoriteErrors.AccessDenied;
		}

		var alreadyFavorited = await favorites.IsFavoritedAsync(owner, identifier, cancellationToken);

		if (alreadyFavorited)
		{
			await favorites.RemoveAsync(owner, identifier, cancellationToken);
			return new FavoriteStateDto(identifier.Value, false);
		}

		var favorite = RecipeFavorite.Create(identifier, owner);
		await favorites.AddAsync(favorite, cancellationToken);
		return new FavoriteStateDto(identifier.Value, true);
	}
}
