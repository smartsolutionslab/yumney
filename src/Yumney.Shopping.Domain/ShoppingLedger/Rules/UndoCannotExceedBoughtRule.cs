using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Shopping.Domain.ShoppingLedger.Rules;

internal sealed class UndoCannotExceedBoughtRule(decimal currentBought, decimal undoAmount) : IBusinessRule
{
	public string Message => "Cannot undo more than the recorded bought quantity.";

	public bool IsBroken() => undoAmount > currentBought;
}
