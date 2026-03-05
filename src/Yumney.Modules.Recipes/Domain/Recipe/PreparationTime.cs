using Yumney.Shared.Guards;

namespace Yumney.Modules.Recipes.Domain.Recipe;

public record PreparationTime
{
    public int Minutes { get; }

    public PreparationTime(int minutes)
    {
        Ensure.That(minutes).IsNotNegative();
        Minutes = minutes;
    }

    public override string ToString()
    {
        return Minutes >= 60
            ? $"{Minutes / 60}h {Minutes % 60}min"
            : $"{Minutes}min";
    }
}
