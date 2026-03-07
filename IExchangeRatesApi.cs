namespace ExchangeRateCollector;

public interface IExchangeRatesApi
{
    Task<Dictionary<string, decimal>> FetchRatesAsync(CancellationToken cancellationToken = default);
}