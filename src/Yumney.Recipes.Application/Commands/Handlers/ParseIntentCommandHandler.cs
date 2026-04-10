using Microsoft.Extensions.Logging;
using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

#pragma warning disable SA1601
public sealed partial class ParseIntentCommandHandler(
    IIntentParserService intentParser,
    ILogger<ParseIntentCommandHandler> logger) : ICommandHandler<ParseIntentCommand, Result<ParsedIntentDto>>
{
    public async Task<Result<ParsedIntentDto>> HandleAsync(ParseIntentCommand command, CancellationToken cancellationToken = default)
    {
        var (message, pageContext) = command;

        LogParseIntent(message.Length, pageContext);

        return await intentParser.ParseAsync(message, pageContext, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Parsing intent, message length {MessageLength}, context {PageContext}")]
    private partial void LogParseIntent(int messageLength, string? pageContext);
}
