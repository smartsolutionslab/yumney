using Serilog;
using Yumney.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults
builder.AddServiceDefaults();

// Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// TODO: Register module services
// builder.Services.AddRecipesModule(builder.Configuration);
// builder.Services.AddShoppingModule(builder.Configuration);
// builder.Services.AddUsersModule(builder.Configuration);

var app = builder.Build();

// Middleware
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// TODO: Map module endpoints
// app.MapRecipeEndpoints();
// app.MapShoppingEndpoints();
// app.MapUserEndpoints();

app.Run();
