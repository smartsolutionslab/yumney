using SmartSolutionsLab.Yumney.Recipes.Domain.Recipe;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Tests.Builders;

public sealed class ImageUrlBuilder
{
	private string value = "https://example.com/image.jpg";

	public static ImageUrlBuilder A() => new();

	public ImageUrlBuilder With(string value)
	{
		this.value = value;
		return this;
	}

	public ImageUrl Build() => ImageUrl.From(value);

	public static implicit operator ImageUrl(ImageUrlBuilder builder) => builder.Build();
}
