using Microsoft.Extensions.Hosting;
using SmartSolutionsLab.Yumney.Shared.AI;
using SmartSolutionsLab.Yumney.Shared.Modules;

namespace SmartSolutionsLab.Yumney.Shopping.Extraction;

public sealed class ShoppingExtractionModule : IModule
{
	public IHostApplicationBuilder RegisterServices(IHostApplicationBuilder builder)
	{
		builder.AddYumneyChatClient();
		builder.Services.AddShoppingExtraction(builder.Configuration);
		return builder;
	}
}
