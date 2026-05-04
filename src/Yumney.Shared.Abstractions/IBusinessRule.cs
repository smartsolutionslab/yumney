namespace SmartSolutionsLab.Yumney.Shared.Abstractions;

public interface IBusinessRule
{
	string Message { get; }

	bool IsBroken();
}
