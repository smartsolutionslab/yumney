using System.Diagnostics.Metrics;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

internal sealed class TestMeterFactory : IMeterFactory
{
	public Meter Create(MeterOptions options) => new(options);

	public void Dispose()
	{
	}
}
