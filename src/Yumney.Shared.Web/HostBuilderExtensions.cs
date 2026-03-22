using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using SmartSolutionsLab.Yumney.ServiceDefaults;
using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.Events;
using SmartSolutionsLab.Yumney.Shared.Persistence;
using SmartSolutionsLab.Yumney.Shared.Web.Middleware;
using SmartSolutionsLab.Yumney.Shared.Web.Services;

namespace SmartSolutionsLab.Yumney.Shared.Web;

public static class HostBuilderExtensions
{
    public static WebApplicationBuilder AddYumneyDefaults(this WebApplicationBuilder builder)
    {
        builder.AddServiceDefaults();

        var realm = builder.Configuration.GetValue<string>("Keycloak:Realm") ?? "yumney";

        var keycloakUrl = builder.Configuration.GetConnectionString("keycloak")
            ?? "http://localhost:8080";

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"{keycloakUrl}/realms/{realm}";
                options.Audience = "yumney-api";
                options.RequireHttpsMetadata = false;
            });

        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUserProvider>();
        builder.Services.AddInProcessEventBus();
        builder.Services.AddScoped<DomainEventDispatchInterceptor>();
        builder.Services.AddOpenApi();

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddSlidingWindowLimiter("RecipeImport", limiter =>
            {
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.SegmentsPerWindow = 4;
                limiter.PermitLimit = 10;
                limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiter.QueueLimit = 2;
            });
        });

        return builder;
    }

    public static WebApplication UseYumneyDefaults(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication()
            .UseAuthorization();

        app.UseRateLimiter();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapDefaultEndpoints();

        return app;
    }
}
