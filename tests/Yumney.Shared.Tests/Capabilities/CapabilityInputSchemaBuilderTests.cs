using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartSolutionsLab.Yumney.Shared.Web.Capabilities;
using Xunit;

namespace SmartSolutionsLab.Yumney.Shared.Tests.Capabilities;

public class CapabilityInputSchemaBuilderTests
{
	private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};

	public enum TestMealType
	{
		Breakfast,
		Dinner,
	}

	[Fact]
	public void Build_NoParameters_ReturnsEmptyObjectSchema()
	{
		var schema = CapabilityInputSchemaBuilder.Build(Description(), SerializerOptions);

		schema.GetProperty("type").GetString().Should().Be("object");
	}

	[Fact]
	public void Build_PathParameter_IsTypedAndRequired()
	{
		var description = Description(Parameter("year", typeof(int), BindingSource.Path, isRequired: false));

		var schema = CapabilityInputSchemaBuilder.Build(description, SerializerOptions);

		schema.GetProperty("properties").GetProperty("year").GetProperty("type").GetString().Should().Be("integer");
		RequiredNames(schema).Should().Equal("year");
	}

	[Fact]
	public void Build_OptionalQueryParameter_IsNotRequired()
	{
		var description = Description(Parameter("search", typeof(string), BindingSource.Query, isRequired: false));

		var schema = CapabilityInputSchemaBuilder.Build(description, SerializerOptions);

		schema.GetProperty("properties").TryGetProperty("search", out _).Should().BeTrue();
		schema.TryGetProperty("required", out _).Should().BeFalse();
	}

	[Fact]
	public void Build_BodyParameter_FlattensPropertiesInCamelCase()
	{
		var description = Description(Parameter("request", typeof(AssignTestMeal), BindingSource.Body, isRequired: true));

		var schema = CapabilityInputSchemaBuilder.Build(description, SerializerOptions);

		schema.GetProperty("properties").EnumerateObject().Select(property => property.Name)
			.Should().BeEquivalentTo(["recipeIdentifier", "recipeTitle", "mealType", "servings"]);
	}

	[Fact]
	public void Build_BodyParameter_MarksNonDefaultedConstructorParametersRequired()
	{
		var description = Description(Parameter("request", typeof(AssignTestMeal), BindingSource.Body, isRequired: true));

		var schema = CapabilityInputSchemaBuilder.Build(description, SerializerOptions);

		RequiredNames(schema).Should().BeEquivalentTo(["recipeIdentifier", "recipeTitle"]);
	}

	[Fact]
	public void Build_BodyEnumProperty_ListsEnumNames()
	{
		var description = Description(Parameter("request", typeof(AssignTestMeal), BindingSource.Body, isRequired: true));

		var schema = CapabilityInputSchemaBuilder.Build(description, SerializerOptions);

		schema.GetProperty("properties").GetProperty("mealType").GetProperty("enum").EnumerateArray()
			.Select(value => value.GetString())
			.Should().Equal("Breakfast", "Dinner");
	}

	[Fact]
	public void Build_ServiceParameter_IsIgnored()
	{
		var description = Description(Parameter("handler", typeof(IDisposable), BindingSource.Services, isRequired: true));

		var schema = CapabilityInputSchemaBuilder.Build(description, SerializerOptions);

		schema.GetProperty("properties").EnumerateObject().Should().BeEmpty();
	}

	[Fact]
	public void Build_PathAndBodyParameters_AreMergedIntoOneObject()
	{
		var description = Description(
			Parameter("year", typeof(int), BindingSource.Path, isRequired: true),
			Parameter("request", typeof(AssignTestMeal), BindingSource.Body, isRequired: true));

		var schema = CapabilityInputSchemaBuilder.Build(description, SerializerOptions);

		RequiredNames(schema).Should().BeEquivalentTo(["year", "recipeIdentifier", "recipeTitle"]);
	}

	private static ApiDescription Description(params ApiParameterDescription[] parameters)
	{
		var description = new ApiDescription { HttpMethod = "POST", ActionDescriptor = new ActionDescriptor() };
		foreach (var parameter in parameters)
		{
			description.ParameterDescriptions.Add(parameter);
		}

		return description;
	}

	private static ApiParameterDescription Parameter(string name, Type type, BindingSource source, bool isRequired) =>
		new() { Name = name, Type = type, Source = source, IsRequired = isRequired };

	private static IEnumerable<string?> RequiredNames(JsonElement schema) =>
		schema.GetProperty("required").EnumerateArray().Select(name => name.GetString());

	public sealed record AssignTestMeal(
		Guid RecipeIdentifier,
		string RecipeTitle,
		TestMealType MealType = TestMealType.Dinner,
		int? Servings = null);
}
