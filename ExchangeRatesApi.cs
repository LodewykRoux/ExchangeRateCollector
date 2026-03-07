using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace ExchangeRateCollector;

public class ExchangeRatesApi : IExchangeRatesApi
{
    private HttpClient _client = new();
    private readonly ILogger<ExchangeRatesApi> _logger;
    private string _exchangeRateApiUrl = Environment.GetEnvironmentVariable("ExchangeRateApiUrl") ?? string.Empty;
    private string _exchangeRateApiAccessKey = Environment.GetEnvironmentVariable("ExchangeRateApiAccessKey") ?? string.Empty;
    private string _exchangeRateApiBaseCurrency = Environment.GetEnvironmentVariable("ExchangeRateApiBaseCurrency") ?? string.Empty;
    private List<string> _exchangeRateApiTargetCurrencies = Environment.GetEnvironmentVariable("ExchangeRateApiTargetCurrencies")?.Split(',').ToList() ?? [];

    public ExchangeRatesApi(ILogger<ExchangeRatesApi> logger)
    {
        _logger = logger;
    }
    
    public async Task<Dictionary<string, decimal>> FetchRatesAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_exchangeRateApiUrl) ||
            string.IsNullOrWhiteSpace(_exchangeRateApiAccessKey) ||
            string.IsNullOrWhiteSpace(_exchangeRateApiBaseCurrency) ||
            _exchangeRateApiTargetCurrencies.Count == 0)
        {
            _logger.LogError(
                "Exchange rate API configuration is invalid. Url configured: {HasUrl}, AccessKey configured: {HasAccessKey}, BaseCurrency configured: {HasBaseCurrency}, TargetCurrencyCount: {TargetCurrencyCount}",
                !string.IsNullOrWhiteSpace(_exchangeRateApiUrl),
                !string.IsNullOrWhiteSpace(_exchangeRateApiAccessKey),
                !string.IsNullOrWhiteSpace(_exchangeRateApiBaseCurrency),
                _exchangeRateApiTargetCurrencies.Count);

            return [];
        }
        
        var symbols = string.Join(
            ",",
            _exchangeRateApiTargetCurrencies
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant()));

        var requestUri =
            $"{_exchangeRateApiUrl}?access_key={Uri.EscapeDataString(_exchangeRateApiAccessKey)}" +
            $"&base={Uri.EscapeDataString(_exchangeRateApiBaseCurrency.Trim().ToUpperInvariant())}" +
            $"&symbols={Uri.EscapeDataString(symbols)}";
        
        try
        {
            _logger.LogInformation(
                "Fetching exchange rates for base currency {BaseCurrency} and symbols {Symbols}",
                _exchangeRateApiBaseCurrency,
                symbols);

            using var response = await _client.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogError(
                    "Exchange rate API request failed. StatusCode: {StatusCode}. Response: {Response}",
                    response.StatusCode,
                    errorBody);

                return [];
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<ExchangeRatesApiResponse>(cancellationToken: cancellationToken);

            if (apiResponse is null)
            {
                _logger.LogError("Exchange rate API returned an empty response body.");
                return [];
            }

            if (!apiResponse.Success)
            {
                _logger.LogError(
                    "Exchange rate API returned unsuccessful response. Error code: {ErrorCode}, Error type: {ErrorType}, Error info: {ErrorInfo}",
                    apiResponse.Error?.Code,
                    apiResponse.Error?.Type,
                    apiResponse.Error?.Info);

                return [];
            }

            if (apiResponse.Rates.Count == 0)
            {
                _logger.LogWarning("Exchange rate API returned no rates.");
                return [];
            }

            _logger.LogInformation(
                "Successfully fetched {RateCount} exchange rates for base currency {BaseCurrency}",
                apiResponse.Rates.Count,
                apiResponse.Base);

            return apiResponse.Rates;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Exchange rate API request timed out.");
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching exchange rates.");
            return [];
        }
    }
    
    private sealed class ExchangeRatesApiResponse
    {
        public bool Success { get; init; }
        public string Base { get; init; } = string.Empty;
        public Dictionary<string, decimal> Rates { get; init; } = [];
        public ExchangeRatesApiError? Error { get; init; }
    }

    private sealed class ExchangeRatesApiError
    {
        public int Code { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
    }
}