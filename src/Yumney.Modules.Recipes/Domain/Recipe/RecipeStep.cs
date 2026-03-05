namespace Yumney.Modules.Recipes.Domain.Recipe;

public class RecipeStep
{
    public RecipeStep(
        Guid id,
        RecipeId recipeId,
        int stepNumber,
        string instruction,
        int? durationMinutes = null)
    {
        Id = id;
        RecipeId = recipeId;
        StepNumber = stepNumber;
        Instruction = instruction;
        DurationMinutes = durationMinutes;
        TimerRequired = durationMinutes.HasValue;
    }

    public Guid Id { get; private set; }

    public RecipeId RecipeId { get; private set; }

    public int StepNumber { get; private set; }

    public string Instruction { get; private set; }

    public int? DurationMinutes { get; private set; }

    public bool TimerRequired { get; private set; }
}
