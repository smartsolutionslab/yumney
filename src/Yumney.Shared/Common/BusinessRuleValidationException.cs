namespace Yumney.Shared.Common;

public class BusinessRuleValidationException : Exception
{
    public BusinessRuleValidationException(IBusinessRule brokenRule)
        : base(brokenRule.Message)
    {
        BrokenRule = brokenRule;
    }

    public IBusinessRule BrokenRule { get; }
}
