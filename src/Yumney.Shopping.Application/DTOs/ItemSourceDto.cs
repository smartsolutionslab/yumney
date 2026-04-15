namespace SmartSolutionsLab.Yumney.Shopping.Application.DTOs;

public sealed record ItemSourceDto(
    decimal Quantity,
    string Source,
    DateTime OccurredAt);
