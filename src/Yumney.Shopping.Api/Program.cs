using SmartSolutionsLab.Yumney.Shared.Hosting;
using SmartSolutionsLab.Yumney.Shopping.Api;
using SmartSolutionsLab.Yumney.Shopping.Application;
using SmartSolutionsLab.Yumney.Shopping.Infrastructure;

ModuleHost.Run(
	args,
	new ShoppingApiModule(),
	new ShoppingApplicationModule(),
	new ShoppingInfrastructureModule());
