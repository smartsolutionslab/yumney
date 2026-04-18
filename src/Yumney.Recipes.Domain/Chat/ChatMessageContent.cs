using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Chat;

public sealed record ChatMessageContent : IValueObject<string>
{
	public const int MaxLength = 4000;

	public string Value { get; }

	private ChatMessageContent(string value)
	{
		string validated = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.AndReturn();
		Value = validated.Trim();
	}

	public static ChatMessageContent From(string value) => new(value);

	public static implicit operator string(ChatMessageContent content) => content.Value;
}
