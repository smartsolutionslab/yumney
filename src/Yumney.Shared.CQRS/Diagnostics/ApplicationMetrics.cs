using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SmartSolutionsLab.Yumney.Shared.CQRS.Diagnostics;

public sealed class ApplicationMetrics
{
	public const string MeterName = "Yumney.Application";

	private readonly Counter<long> handlerExecuted;
	private readonly Histogram<double> handlerDuration;

	public ApplicationMetrics(IMeterFactory meterFactory)
	{
		var meter = meterFactory.Create(MeterName);
		handlerExecuted = meter.CreateCounter<long>("yumney.handlers.executed", description: "Number of handler executions");
		handlerDuration = meter.CreateHistogram<double>("yumney.handlers.duration", "ms", "Handler execution duration");
	}

	public void RecordExecution(string handlerName, string commandType, string result, double durationMs)
	{
		var tags = new TagList
		{
			{ "handler_name", handlerName },
			{ "command_type", commandType },
			{ "result", result },
		};

		handlerExecuted.Add(1, tags);
		handlerDuration.Record(durationMs, tags);
	}
}
