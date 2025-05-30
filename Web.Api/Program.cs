using System.Reflection;
using Application;
using HealthChecks.UI.Client;
using Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Web.Api;
using Web.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddSwaggerGenWithAuth();

builder.Services
    .AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

WebApplication app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
    app.UseSwaggerWithUi();
    app.ApplyMigrations();
}
else
{
    app.UseCors("Production");
    app.UseSwaggerWithUi();
    app.ApplyMigrations();
}

app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();

    app.ApplyMigrations();
}

app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseRequestContextLogging();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

app.UseAuthentication();

app.UseAuthorization();


await app.RunAsync();

//dotnet ef migrations add modelupdate --project Infrastructure --startup-project Web.Api
namespace Web.Api
{
    public partial class Program;
}
