using System.Diagnostics.Metrics;
using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.CQRS.Diagnostics;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.CQRS;

public class ApplicationMetricsTests
{
	[Fact]
	public void RecordExecution_IncrementsExecutedCounterWithTags()
	{
		using var listener = new MeterListener();
		List<(long Value, KeyValuePair<string, object?>[] Tags)> samples = [];
		listener.InstrumentPublished = (instrument, l) =>
		{
			if (instrument.Meter.Name == ApplicationMetrics.MeterName
				&& instrument.Name == "yumney.handlers.executed")
			{
				l.EnableMeasurementEvents(instrument);
			}
		};
		listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
			samples.Add((measurement, tags.ToArray())));
		listener.Start();

		var metrics = new ApplicationMetrics(new TestMeterFactory());
		metrics.RecordExecution("ImportHandler", "ImportRecipeCommand", "success", durationMs: 12.5);

		samples.Should().ContainSingle();
		samples[0].Value.Should().Be(1);
		samples[0].Tags.Should().Contain(tag => tag.Key == "handler_name" && (string)tag.Value! == "ImportHandler");
		samples[0].Tags.Should().Contain(tag => tag.Key == "command_type" && (string)tag.Value! == "ImportRecipeCommand");
		samples[0].Tags.Should().Contain(tag => tag.Key == "result" && (string)tag.Value! == "success");
	}

	[Fact]
	public void RecordExecution_RecordsHistogramWithDurationAndTags()
	{
		using var listener = new MeterListener();
		List<(double Value, KeyValuePair<string, object?>[] Tags)> samples = [];
		listener.InstrumentPublished = (instrument, l) =>
		{
			if (instrument.Meter.Name == ApplicationMetrics.MeterName
				&& instrument.Name == "yumney.handlers.duration")
			{
				l.EnableMeasurementEvents(instrument);
			}
		};
		listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
			samples.Add((measurement, tags.ToArray())));
		listener.Start();

		var metrics = new ApplicationMetrics(new TestMeterFactory());
		metrics.RecordExecution("RateHandler", "RateRecipeCommand", "failure", durationMs: 42.3);

		samples.Should().ContainSingle();
		samples[0].Value.Should().Be(42.3);
		samples[0].Tags.Should().Contain(tag => tag.Key == "result" && (string)tag.Value! == "failure");
	}

	[Fact]
	public void MeterName_IsTheCanonicalYumneyApplicationConstant()
	{
		ApplicationMetrics.MeterName.Should().Be("Yumney.Application");
	}
}
