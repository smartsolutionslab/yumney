using System;

namespace SmartSolutionsLab.Yumney.Shared.Common;

public sealed class BusinessRuleValidationException(IBusinessRule brokenRule) : Exception(brokenRule.Message)
{
	public IBusinessRule BrokenRule { get; } = brokenRule;
}
