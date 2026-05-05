using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;

#pragma warning disable SA1311
public sealed record VoiceSpeed : IValueObject<string>
{
	public const int MaxLength = 10;

#pragma warning disable SA1202
	private static readonly string[] allowedValues = ["slow", "normal", "fast"];

	public static readonly VoiceSpeed Slow = new("slow");
	public static readonly VoiceSpeed Normal = new("normal");
	public static readonly VoiceSpeed Fast = new("fast");
#pragma warning restore SA1202

	public string Value { get; }

	private VoiceSpeed(string value)
	{
		Value = Ensure.That(value)
			.IsNotNullOrWhiteSpace()
			.HasMaxLength(MaxLength)
			.IsOneOf(allowedValues)
			.AndReturn();
	}

	public static VoiceSpeed From(string value) => new(value);

	public static implicit operator string(VoiceSpeed obj) => obj.Value;
}
#pragma warning restore SA1311
