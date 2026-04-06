using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

public sealed record ActivityType : IValueObject<string>
{
    public static readonly ActivityType RecipeImported = new("recipe_imported");
    public static readonly ActivityType RecipeViewed = new("recipe_viewed");
    public static readonly ActivityType RecipeEdited = new("recipe_edited");
    public static readonly ActivityType RecipeDeleted = new("recipe_deleted");
    public static readonly ActivityType ShoppingListCreated = new("shopping_list_created");

    public string Value { get; }

    private ActivityType(string value)
    {
        Value = Ensure.That(value).IsNotNullOrWhiteSpace().AndReturn();
    }

    public static ActivityType From(string value)
    {
        Ensure.That(value).IsNotNullOrWhiteSpace();
        return new ActivityType(value);
    }

    public static implicit operator string(ActivityType type) => type.Value;
}
