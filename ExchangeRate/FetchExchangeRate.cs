using ExchangeRateCollector.ExchangeRate.Interface;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExchangeRateCollector.ExchangeRate;

public class FetchExchangeRate(
    ILoggerFactory loggerFactory,
    IExchangeRatesApi exchangeRatesApi,
    IExchangeRateDb exchangeRatesDb)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<FetchExchangeRate>();

    [Function("FetchExchangeRate")]
    public async Task Run([TimerTrigger("0 0 2 * * *", RunOnStartup = true)] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);

        var exchangeRates = await exchangeRatesApi.FetchRatesAsync();

        await exchangeRatesDb.SaveExchangeRateAsync(exchangeRates);
        
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }
}