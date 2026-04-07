using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Chat;

public sealed record ChatRole : IValueObject<string>
{
#pragma warning disable SA1202 // AllowedValues must initialize before the public static instances
    private static readonly string[] AllowedValues = ["user", "assistant"];

    public static readonly ChatRole User = new("user");
    public static readonly ChatRole Assistant = new("assistant");
#pragma warning restore SA1202

    public string Value { get; }

    private ChatRole(string value)
    {
        Value = Ensure.That(value).IsNotNullOrWhiteSpace().IsOneOf(AllowedValues).AndReturn();
    }

    public static ChatRole From(string value)
    {
        string validated = Ensure.That(value).IsNotNullOrWhiteSpace().AndReturn();
        return new ChatRole(validated.ToLowerInvariant());
    }

    public static implicit operator string(ChatRole role) => role.Value;
}
