using System.Buffers.Text;
using System.Globalization;
using System.Text;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using SmartSolutionsLab.Yumney.Shared.Guards;

namespace SmartSolutionsLab.Yumney.Users.Domain.UserActivity;

/// <summary>
/// Opaque keyset cursor for the activity timeline: an (OccurredAt, Identifier)
/// pair encoded for transport. The identifier is the tie-breaker when two
/// activity rows share a millisecond-aligned timestamp.
/// </summary>
public sealed record ActivityCursor : IValueObject
{
	public DateTime OccurredAt { get; }

	public UserActivityIdentifier TieBreaker { get; }

	private ActivityCursor(DateTime occurredAt, UserActivityIdentifier tieBreaker)
	{
		OccurredAt = occurredAt;
		TieBreaker = tieBreaker;
	}

	public static ActivityCursor From(DateTime occurredAt, UserActivityIdentifier tieBreaker) =>
		new(occurredAt, Ensure.That(tieBreaker).IsNotNull().AndReturn());

	public string Encode()
	{
		var ticks = OccurredAt.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture);
		var raw = $"{ticks}|{TieBreaker.Value}";
		return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(raw));
	}

	public static ActivityCursor? TryDecode(string? encoded)
	{
		if (string.IsNullOrWhiteSpace(encoded)) return null;

		try
		{
			var raw = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(encoded));
			var separator = raw.IndexOf('|');
			if (separator <= 0 || separator == raw.Length - 1) return null;

			if (!long.TryParse(raw.AsSpan(0, separator), CultureInfo.InvariantCulture, out var ticks)) return null;
			if (!Guid.TryParse(raw.AsSpan(separator + 1), out var tieBreaker)) return null;

			return new ActivityCursor(new DateTime(ticks, DateTimeKind.Utc), UserActivityIdentifier.From(tieBreaker));
		}
		catch (FormatException)
		{
			return null;
		}
	}
}
