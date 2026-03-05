namespace Yumney.Modules.Recipes.Domain.Recipe;

public record RecipeId(Guid Value)
{
    public static RecipeId New() => new(Guid.NewGuid());

    public static implicit operator Guid(RecipeId id) => id.Value;

    public override string ToString() => Value.ToString();
}
