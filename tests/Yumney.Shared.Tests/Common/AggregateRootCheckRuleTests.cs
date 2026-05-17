using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Abstractions;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class AggregateRootCheckRuleTests
{
	private sealed class AlwaysBrokenRule : IBusinessRule
	{
		public string Message => "always-broken";

		public bool IsBroken() => true;
	}

	private sealed class AlwaysSatisfiedRule : IBusinessRule
	{
		public string Message => "always-satisfied";

		public bool IsBroken() => false;
	}

	private sealed class FakeAggregate : AggregateRoot<Guid>
	{
		public FakeAggregate() => Id = Guid.NewGuid();

		public static void Enforce(IBusinessRule rule) => CheckRule(rule);
	}

	[Fact]
	public void CheckRule_BrokenRule_ThrowsBusinessRuleValidationException()
	{
		var rule = new AlwaysBrokenRule();

		var act = () => FakeAggregate.Enforce(rule);

		act.Should().Throw<BusinessRuleValidationException>()
			.Which.BrokenRule.Should().BeSameAs(rule);
	}

	[Fact]
	public void CheckRule_SatisfiedRule_DoesNotThrow()
	{
		var act = () => FakeAggregate.Enforce(new AlwaysSatisfiedRule());

		act.Should().NotThrow();
	}
}
