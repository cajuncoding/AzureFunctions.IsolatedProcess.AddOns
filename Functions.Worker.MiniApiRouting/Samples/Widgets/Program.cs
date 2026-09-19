using Functions.Worker.AddOns.MiniApiRouting;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services
    .AddFunctionsMiniApiRouting()
    .AddSingleton<IWidgetService, WidgetService>();

builder
    .Build()
    .Run();
