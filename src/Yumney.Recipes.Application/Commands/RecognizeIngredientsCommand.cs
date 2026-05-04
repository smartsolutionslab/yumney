using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record RecognizeIngredientsCommand(PhotoData Photo)
	: ICommand<Result<RecognizedIngredientsResponseDto>>;
