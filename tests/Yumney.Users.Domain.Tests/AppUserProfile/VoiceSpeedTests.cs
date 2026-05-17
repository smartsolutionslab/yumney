using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class VoiceSpeedTests
{
	[Fact]
	public void Slow_StaticAccessor_HasSlowValue()
	{
		VoiceSpeed.Slow.Value.Should().Be("slow");
	}

	[Fact]
	public void Normal_StaticAccessor_HasNormalValue()
	{
		VoiceSpeed.Normal.Value.Should().Be("normal");
	}

	[Fact]
	public void Fast_StaticAccessor_HasFastValue()
	{
		VoiceSpeed.Fast.Value.Should().Be("fast");
	}

	[Theory]
	[InlineData("slow")]
	[InlineData("normal")]
	[InlineData("fast")]
	public void From_AllowedValue_IsAccepted(string raw)
	{
		var speed = VoiceSpeed.From(raw);

		speed.Value.Should().Be(raw);
	}

	[Theory]
	[InlineData("turbo")]
	[InlineData("medium")]
	[InlineData("SLOW")]
	public void From_DisallowedValue_Throws(string raw)
	{
		var act = () => VoiceSpeed.From(raw);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void From_Empty_Throws()
	{
		var act = () => VoiceSpeed.From(string.Empty);

		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void ImplicitConversion_ToString_YieldsValue()
	{
		string raw = VoiceSpeed.Normal;

		raw.Should().Be("normal");
	}

	[Fact]
	public void Equality_SameValue_AreEqual()
	{
		var a = VoiceSpeed.From("fast");
		var b = VoiceSpeed.From("fast");

		a.Should().Be(b);
		a.GetHashCode().Should().Be(b.GetHashCode());
	}
}
