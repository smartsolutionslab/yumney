using SmartSolutionsLab.Yumney.Shared.Hosting;
using SmartSolutionsLab.Yumney.Users.Api;
using SmartSolutionsLab.Yumney.Users.Application;
using SmartSolutionsLab.Yumney.Users.Infrastructure;

ModuleHost.Run(
	args,
	new UsersApiModule(),
	new UsersApplicationModule(),
	new UsersInfrastructureModule());
