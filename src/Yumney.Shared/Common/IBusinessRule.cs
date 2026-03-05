namespace Yumney.Shared.Common;

public interface IBusinessRule
{
    string Message { get; }

    bool IsBroken();
}
