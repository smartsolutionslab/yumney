using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Web;
using SmartSolutionsLab.Yumney.Users.Api;
using SmartSolutionsLab.Yumney.Users.Application.Commands;
using SmartSolutionsLab.Yumney.Users.Application.Interfaces;
using SmartSolutionsLab.Yumney.Users.Domain.AppUserProfile;
using SmartSolutionsLab.Yumney.Users.Infrastructure;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Persistence;
using SmartSolutionsLab.Yumney.Users.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddYumneyDefaults();

builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("usersdb"),
        x => x.MigrationsHistoryTable("__UsersMigrationsHistory")));

builder.Services.AddScoped<IAppUserProfileRepository, AppUserProfileRepository>();

builder.Services.AddValidatorsFromAssemblyContaining<RegisterUserRequestValidator>();
builder.Services.AddScoped<ICommandHandler<RegisterUserCommand, Result<RegisterUserResultDto>>, RegisterUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ResendVerificationEmailCommand, Result>, ResendVerificationEmailCommandHandler>();

builder.Services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>(client =>
{
    client.BaseAddress = new Uri("https+http://keycloak");
}).AddStandardResilienceHandler();

var app = builder.Build();

app.UseYumneyDefaults();

app.MapGroup("/api/v1")
    .RequireAuthorization()
    .MapUsersEndpoints();

app.Run();
