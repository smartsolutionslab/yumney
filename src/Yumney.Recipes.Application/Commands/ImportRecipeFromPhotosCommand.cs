using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands;

public sealed record ImportRecipeFromPhotosCommand(
	IReadOnlyList<PhotoData> Photos) : ICommand<Result<ExtractedRecipeDto>>;
