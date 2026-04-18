using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingList;

namespace SmartSolutionsLab.Yumney.Shopping.Application;

public static class CurrentUserExtensions
{
	public static OwnerIdentifier AsOwner(this ICurrentUser currentUser) =>
		OwnerIdentifier.From(currentUser.UserId);
}
