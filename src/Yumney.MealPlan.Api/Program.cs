using SmartSolutionsLab.Yumney.MealPlan.Api;
using SmartSolutionsLab.Yumney.MealPlan.Application;
using SmartSolutionsLab.Yumney.MealPlan.Infrastructure;
using SmartSolutionsLab.Yumney.Shared.Hosting;

ModuleHost.Run(
	args,
	new MealPlanApiModule(),
	new MealPlanApplicationModule(),
	new MealPlanInfrastructureModule());
