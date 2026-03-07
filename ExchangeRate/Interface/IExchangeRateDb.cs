namespace ExchangeRateCollector.ExchangeRate.Interface;

public interface IExchangeRateDb
{
    Task SaveExchangeRateAsync(Dictionary<string, decimal> exchangeRates);
}