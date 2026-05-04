using FluentAssertions;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using Xunit;

namespace SmartSolutionsLab.Yumney.Users.Domain.Tests.AppUserProfile;

public class NotificationPreferencesTests
{
	[Fact]
	public void Default_HasBothChannelsEnabled()
	{
		NotificationPreferences.Default.TimerHapticFeedback.Should().BeTrue();
		NotificationPreferences.Default.TimerSoundAlerts.Should().BeTrue();
	}

	[Fact]
	public void Equality_SameValues_AreEqual()
	{
		var a = new NotificationPreferences(false, true);
		var b = new NotificationPreferences(false, true);

		a.Should().Be(b);
	}

	[Fact]
	public void Equality_DifferentValues_AreNotEqual()
	{
		var a = new NotificationPreferences(true, false);
		var b = new NotificationPreferences(false, false);

		a.Should().NotBe(b);
	}
}
