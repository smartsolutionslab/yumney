using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Guards;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class VoiceSettingsTests
{
	[Theory]
	[InlineData("slow")]
	[InlineData("normal")]
	[InlineData("fast")]
	public void VoiceSpeed_From_AcceptsKnownValues(string value)
	{
		VoiceSpeed.From(value).Value.Should().Be(value);
	}

	[Theory]
	[InlineData("warp")]
	[InlineData("Normal")]
	public void VoiceSpeed_From_RejectsUnknownValues(string value)
	{
		var act = () => VoiceSpeed.From(value);
		act.Should().Throw<GuardException>();
	}

	[Fact]
	public void Default_HasSensibleDefaults()
	{
		VoiceSettings.Default.Enabled.Should().BeTrue();
		VoiceSettings.Default.Speed.Should().Be(VoiceSpeed.Normal);
		VoiceSettings.Default.AutoReadInCookMode.Should().BeFalse();
	}

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var a = new VoiceSettings(true, VoiceSpeed.Fast, true);
		var b = new VoiceSettings(true, VoiceSpeed.Fast, true);

		a.Should().Be(b);
	}
}
