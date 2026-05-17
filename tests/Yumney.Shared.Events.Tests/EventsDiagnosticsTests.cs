using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Events;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Events.Tests;

public class EventsDiagnosticsTests
{
	[Fact]
	public void SourceName_IsTheCanonicalYumneyEventsConstant()
	{
		EventsDiagnostics.SourceName.Should().Be("Yumney.Events");
	}

	[Fact]
	public void ActivitySource_NameMatchesSourceName()
	{
		EventsDiagnostics.ActivitySource.Name.Should().Be(EventsDiagnostics.SourceName);
	}
}
