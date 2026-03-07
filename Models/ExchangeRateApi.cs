namespace ExchangeRateCollector.Models;

public class ExchangeRateApi
{
    public string ExchangeRateApiUrl { get; set; }
    public string ExchangeRateApiAccessKey { get; set; }
    public string ExchangeRateApiBaseCurrency { get; set; }
    public string ExchangeRateApiTargetCurrencies { get; set; }
}