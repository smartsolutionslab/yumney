using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Infrastructure.Persistence.Converters;

internal sealed class StepIdentifierConverter()
	: ValueConverter<StepIdentifier, Guid>(v => v.Value, v => StepIdentifier.From(v));
