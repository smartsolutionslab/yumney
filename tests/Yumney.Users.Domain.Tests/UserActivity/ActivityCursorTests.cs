using FluentAssertions;
using SmartSolutionsLab.Yumney.Users.Domain.UserActivity;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.UserActivity;

public class ActivityCursorTests
{
	[Fact]
	public void Encode_Decode_RoundTripsExactValues()
	{
		var occurredAt = new DateTime(2026, 5, 7, 12, 34, 56, DateTimeKind.Utc).AddTicks(123);
		var tieBreaker = UserActivityIdentifier.New();
		var cursor = ActivityCursor.From(occurredAt, tieBreaker);

		var decoded = ActivityCursor.TryDecode(cursor.Encode());

		decoded.Should().NotBeNull();
		decoded!.OccurredAt.Should().Be(occurredAt);
		decoded.TieBreaker.Should().Be(tieBreaker);
	}

	[Fact]
	public void TryDecode_Null_ReturnsNull()
	{
		ActivityCursor.TryDecode(null).Should().BeNull();
	}

	[Fact]
	public void TryDecode_Empty_ReturnsNull()
	{
		ActivityCursor.TryDecode(string.Empty).Should().BeNull();
	}

	[Fact]
	public void TryDecode_Garbage_ReturnsNull()
	{
		ActivityCursor.TryDecode("not-a-valid-cursor!!!").Should().BeNull();
	}

	[Fact]
	public void TryDecode_MissingSeparator_ReturnsNull()
	{
		// Base64 of "1234567890" — no '|' separator
		ActivityCursor.TryDecode("MTIzNDU2Nzg5MA").Should().BeNull();
	}
}
