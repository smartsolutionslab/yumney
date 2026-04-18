namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public sealed record AddedItemDto(
	string ItemName,
	decimal Quantity,
	string? Unit,
	string Category,
	string Source,
	Guid LedgerIdentifier);
