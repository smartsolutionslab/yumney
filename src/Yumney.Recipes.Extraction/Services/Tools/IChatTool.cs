using Microsoft.Extensions.AI;

namespace SmartSolutionsLab.Yumney.Recipes.Extraction.Services.Tools;

/// <summary>
/// A function the in-app chat model may call. The tool's method carries the
/// <see cref="System.ComponentModel.DescriptionAttribute"/>s the model sees;
/// the wire name matches the MCP capability name where one exists.
/// </summary>
public interface IChatTool
{
	/// <summary>Expose the tool to the chat model.</summary>
	/// <returns>The tool as an invocable function bound to this instance.</returns>
	AIFunction ToFunction();
}
