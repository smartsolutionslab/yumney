using FluentAssertions;
using SmartSolutionsLab.Yumney.Shared.Common;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Common;

public class BusinessRuleValidationExceptionTests
{
	private sealed class TestRule(string message) : IBusinessRule
	{
		public string Message => message;

		public bool IsBroken() => true;
	}

	[Fact]
	public void Constructor_SetsMessage()
	{
		var rule = new TestRule("Something went wrong");

		var exception = new BusinessRuleValidationException(rule);

		exception.Message.Should().Be("Something went wrong");
	}

	[Fact]
	public void Constructor_SetsBrokenRule()
	{
		var rule = new TestRule("Rule violated");

		var exception = new BusinessRuleValidationException(rule);

		exception.BrokenRule.Should().BeSameAs(rule);
	}

	[Fact]
	public void IsException()
	{
		var rule = new TestRule("Error");

		var exception = new BusinessRuleValidationException(rule);

		exception.Should().BeAssignableTo<Exception>();
	}
}
