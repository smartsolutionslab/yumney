using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

public sealed record ActivityType : IValueObject<string>
{
#pragma warning disable SA1202 // AllowedValues must initialize before the public static instances
    private static readonly string[] AllowedValues =
    [
        "recipe_imported",
        "recipe_viewed",
        "recipe_edited",
        "recipe_deleted",
        "shopping_list_created",
    ];

    public static readonly ActivityType RecipeImported = new("recipe_imported");
    public static readonly ActivityType RecipeViewed = new("recipe_viewed");
    public static readonly ActivityType RecipeEdited = new("recipe_edited");
    public static readonly ActivityType RecipeDeleted = new("recipe_deleted");
    public static readonly ActivityType ShoppingListCreated = new("shopping_list_created");
#pragma warning restore SA1202

    public string Value { get; }

    private ActivityType(string value)
    {
        Value = Ensure.That(value).IsNotNullOrWhiteSpace().IsOneOf(AllowedValues).AndReturn();
    }

    public static ActivityType From(string value) => new(value);

    public static implicit operator string(ActivityType type) => type.Value;
}
