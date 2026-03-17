using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using SmartSolutionsLab.Yumney.ServiceDefaults;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Web.Middleware;
using SmartSolutionsLab.Yumney.Shared.Web.Services;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class HostBuilderExtensions
{
    public static WebApplicationBuilder AddYumneyDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        var realm = builder.Configuration.GetValue<string>("Keycloak:Realm") ?? "yumney";

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddKeycloakJwtBearer(
                serviceName: "keycloak",
                realm: realm,
                configureOptions: options =>
                {
                    options.Authority = $"http://keycloak/realms/{realm}";
                    options.RequireHttpsMetadata = false;
                    options.Audience = "yumney-api";
                });

        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUserService>();
        builder.Services.AddInProcessEventBus();
        builder.Services.AddOpenApi();

        return builder;
    }

    public static WebApplication UseYumneyDefaults(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        app.UseAuthentication()
            .UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapDefaultEndpoints();

        return app;
    }
}
