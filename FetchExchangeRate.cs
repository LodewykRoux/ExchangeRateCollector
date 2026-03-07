using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ExchangeRateCollector;

public class FetchExchangeRate
{
    private readonly ILogger _logger;
    private IExchangeRatesApi _exchangeRatesApi;

    public FetchExchangeRate(ILoggerFactory loggerFactory, IExchangeRatesApi exchangeRatesApi)
    {
        _logger = loggerFactory.CreateLogger<FetchExchangeRate>();
        _exchangeRatesApi = exchangeRatesApi;
    }

    [Function("FetchExchangeRate")]
    public async Task Run([TimerTrigger("0 0 2 * * *", RunOnStartup = true)] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);

        var exchangeRates = await _exchangeRatesApi.FetchRatesAsync();
        
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }
}