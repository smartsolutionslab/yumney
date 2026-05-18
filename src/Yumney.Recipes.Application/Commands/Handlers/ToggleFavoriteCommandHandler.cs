using Microsoft.EntityFrameworkCore;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Recipes.Domain.RecipeFavorite;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Shared.Persistence.EventStore;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class ToggleFavoriteCommandHandler(IRecipesUnitOfWork unitOfWork, ICurrentUser currentUser)
	: ICommandHandler<ToggleFavoriteCommand, Result<FavoriteStateDto>>
{
	public async Task<Result<FavoriteStateDto>> HandleAsync(ToggleFavoriteCommand command, CancellationToken cancellationToken = default)
	{
		var identifier = command.Identifier;
		var owner = currentUser.AsOwner();

		var recipe = await unitOfWork.Recipes.GetByIdAsync(identifier, cancellationToken);

		if (recipe.Owner != owner) return ToggleFavoriteErrors.AccessDenied;

		var alreadyFavorited = await unitOfWork.Favorites.IsFavoritedAsync(owner, identifier, cancellationToken);

		if (alreadyFavorited)
		{
			await unitOfWork.Favorites.RemoveAsync(owner, identifier, cancellationToken);
			await unitOfWork.SaveChangesAsync(cancellationToken);
			return identifier.ToFavoriteStateDto(isFavorite: false);
		}

		var favorite = RecipeFavorite.Create(identifier, owner);
		await unitOfWork.Favorites.AddAsync(favorite, cancellationToken);

		try
		{
			await unitOfWork.SaveChangesAsync(cancellationToken);
		}
		catch (DbUpdateException ex) when (ex.IsUniqueViolation())
		{
			// Concurrent peer inserted the same (Owner, RecipeIdentifier) pair
			// between our IsFavoritedAsync read and our SaveChanges. The
			// favorite now exists either way — same end state as if our insert
			// had committed first — so report the toggle outcome as "now
			// favorited" instead of bubbling a 500 to the caller.
		}

		return identifier.ToFavoriteStateDto(isFavorite: true);
	}
}
