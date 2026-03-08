using Microsoft.Extensions.Logging;
using Yumney.Shared.Common;
using Yumney.Shared.CQRS;

namespace Yumney.Recipes.Application.Commands;

#pragma warning disable SA1601 // Partial elements should be documented (required for LoggerMessage source generation)
public sealed partial class ImportRecipeCommandHandler(ILogger<ImportRecipeCommandHandler> logger)
    : ICommandHandler<ImportRecipeCommand, Result<ImportRecipeResultDto>>
{
    public Task<Result<ImportRecipeResultDto>> HandleAsync(
        ImportRecipeCommand command,
        CancellationToken cancellationToken = default)
    {
        var url = command.Url;

        LogImportAttempt(url.Value);

        var result = Result<ImportRecipeResultDto>.Success(
            new ImportRecipeResultDto("Recipe URL accepted. Extraction will be available in a future release."));

        return Task.FromResult(result);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Recipe import requested for URL {SourceUrl}")]
    private partial void LogImportAttempt(string sourceUrl);
}
