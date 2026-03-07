using ExchangeRateCollector;
using ExchangeRateCollector.ExchangeRate;
using ExchangeRateCollector.ExchangeRate.Interface;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSingleton<IExchangeRatesApi, ExchangeRatesApi>();
builder.Services.AddSingleton<IExchangeRateDb, ExchangeRateDb>();

builder.Build().Run();