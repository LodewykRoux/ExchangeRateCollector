namespace ExchangeRateCollector.ExchangeRate.Interface;

public interface IExchangeRatesApi
{
    Task<Dictionary<string, decimal>> FetchRatesAsync(CancellationToken cancellationToken = default);
}