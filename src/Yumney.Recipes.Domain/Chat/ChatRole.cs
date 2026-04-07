using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Chat;

#pragma warning disable SA1311 // editorconfig requires camelCase for private fields
public sealed record ChatRole : IValueObject<string>
{
#pragma warning disable SA1202 // allowedValues must initialize before the public static instances
    private static readonly string[] allowedValues = ["user", "assistant"];

    public static readonly ChatRole User = new("user");
    public static readonly ChatRole Assistant = new("assistant");
#pragma warning restore SA1202

    public string Value { get; }

    private ChatRole(string value)
    {
        Value = Ensure.That(value).IsNotNullOrWhiteSpace().IsOneOf(allowedValues).AndReturn();
    }

    public static ChatRole From(string value)
    {
        string validated = Ensure.That(value).IsNotNullOrWhiteSpace().AndReturn();
        return new ChatRole(validated.ToLowerInvariant());
    }

    public static implicit operator string(ChatRole role) => role.Value;
}
#pragma warning restore SA1311
