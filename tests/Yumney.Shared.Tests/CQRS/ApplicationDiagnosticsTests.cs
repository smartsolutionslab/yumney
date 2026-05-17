using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.CQRS.Diagnostics;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public class ApplicationDiagnosticsTests
{
	[Fact]
	public void SourceName_IsTheCanonicalYumneyApplicationConstant()
	{
		ApplicationDiagnostics.SourceName.Should().Be("Yumney.Application");
	}

	[Fact]
	public void ActivitySource_NameMatchesSourceName()
	{
		ApplicationDiagnostics.ActivitySource.Name.Should().Be(ApplicationDiagnostics.SourceName);
	}
}
