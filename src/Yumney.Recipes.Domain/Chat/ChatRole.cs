using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Chat;

public sealed record ChatRole : IValueObject<string>
{
    public static readonly ChatRole User = new("user");
    public static readonly ChatRole Assistant = new("assistant");

    public string Value { get; }

    private ChatRole(string value)
    {
        Value = Ensure.That(value).IsNotNullOrWhiteSpace().AndReturn();
    }

    public static ChatRole From(string value)
    {
        string validated = Ensure.That(value).IsNotNullOrWhiteSpace().AndReturn();
        var normalized = validated.ToLowerInvariant();
        return normalized switch
        {
            "user" => User,
            "assistant" => Assistant,
            _ => throw new ArgumentException($"Unknown chat role: {value}", nameof(value)),
        };
    }

    public static implicit operator string(ChatRole role) => role.Value;
}
