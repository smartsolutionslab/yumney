using System;

namespace SmartSolutionsLab.Yumney.Shared.Abstractions;

public sealed class BusinessRuleValidationException(IBusinessRule brokenRule) : Exception(brokenRule.Message)
{
	public IBusinessRule BrokenRule { get; } = brokenRule;
}
