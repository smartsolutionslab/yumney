using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;
using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Recipes.Application;

public static class CurrentUserExtensions
{
	public static OwnerIdentifier AsOwner(this ICurrentUser currentUser) =>
		OwnerIdentifier.From(currentUser.UserId);
}
