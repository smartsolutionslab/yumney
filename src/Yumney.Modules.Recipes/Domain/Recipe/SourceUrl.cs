using Yumney.Shared.Guards;

namespace Yumney.Modules.Recipes.Domain.Recipe;

public record SourceUrl
{
    public string Value { get; }

    public SourceUrl(string value)
    {
        Value = Ensure.That(value)
            .IsValidUrl()
            .HasMaxLength(2000)
            .AndReturn();
    }

    public static implicit operator string(SourceUrl url) => url.Value;
}
