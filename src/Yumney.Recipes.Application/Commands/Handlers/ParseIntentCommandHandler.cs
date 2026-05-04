using SmartSolutionsLab.Yumney.Recipes.Application.DTOs;
using SmartSolutionsLab.Yumney.Recipes.Application.Interfaces;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;

namespace SmartSolutionsLab.Yumney.Recipes.Application.Commands.Handlers;

public sealed class ParseIntentCommandHandler(
	IIntentParserService intentParser) : ICommandHandler<ParseIntentCommand, Result<ParsedIntentDto>>
{
	public async Task<Result<ParsedIntentDto>> HandleAsync(ParseIntentCommand command, CancellationToken cancellationToken = default)
	{
		var (message, pageContext) = command;

		return await intentParser.ParseAsync(message, pageContext, cancellationToken);
	}
}
